using System;
using System.Threading.Tasks;
using MapsterMapper;
using eCommerce.Storefront.Services.Interfaces;
using eCommerce.Storefront.Services.Messaging.OrderService;
using eCommerce.Storefront.Services.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using eCommerce.Storefront.Controllers.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using eCommerce.Storefront.Model.Orders;

namespace eCommerce.Storefront.Controllers.Controllers
{
    public class PaymentController(IPaymentService paymentService,
        IOrderService orderService,
        IMapper mapper,
        ILogger<PaymentController> logger,
        ICookieAuthentication cookieAuthentication) : Controller
    {
        private readonly IPaymentService _paymentService = paymentService;
        private readonly IOrderService _orderService = orderService;
        private readonly IMapper _mapper = mapper;
        private readonly ILogger<PaymentController> _logger = logger;
        private readonly ICookieAuthentication _cookieAuthentication = cookieAuthentication;

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> PaymentCallBack(IFormCollection collection)
        {
            long orderId;

            try
            {
                orderId = _paymentService.GetOrderIdFor(collection);
            }
            catch (FormatException ex)
            {
                // A malformed or missing 'custom' field means we cannot identify the order.
                // Always return 200 OK so PayPal stops retrying; log for audit.
                _logger.LogWarning(ex, "PaymentCallBack: received IPN with missing or invalid 'custom' field.");

                return Ok();
            }

            var request = new GetOrderRequest
            {
                OrderId = orderId
            };
            var response = await _orderService.GetOrderAsync(request);

            if (response?.Order == null)
            {
                _logger.LogError("PaymentCallBack: Order {OrderId} could not be retrieved.", orderId);

                return Ok();
            }

            var orderPaymentRequest = _mapper.Map<OrderView, OrderPaymentRequest>(response.Order);
            var transactionResult = await _paymentService.HandleCallBackAsync(orderPaymentRequest, collection);

            if (transactionResult.PaymentOk)
            {
                var paymentRequest = new SetOrderPaymentRequest
                {
                    Amount = transactionResult.Amount,
                    PaymentToken = transactionResult.PaymentToken,
                    PaymentMerchant = transactionResult.PaymentMerchant,
                    OrderId = orderId,
                    CustomerEmail = response.Order.CustomerEmail
                };

                try
                {
                    await _orderService.SetOrderPaymentAsync(paymentRequest);
                }
                catch (OrderAlreadyPaidForException ex)
                {
                    // Duplicate IPN from PayPal — idempotent ack so PayPal stops retrying.
                    _logger.LogWarning(ex, "Duplicate IPN for order {OrderId}; acknowledging.", orderId);

                    return Ok();
                }
                catch (PaymentAmountDoesNotEqualOrderTotalException ex)
                {
                    _logger.LogError(ex, "IPN amount mismatch for order {OrderId}; rejecting.", orderId);

                    return Ok();
                }

                return Ok();
            }
            else
            {
                _logger.LogWarning("Payment not ok for order id {OrderId}, payment token {PaymentToken}", orderId, transactionResult.PaymentToken);

                // Always 200 OK to stop PayPal retries; the order remains unpaid.
                return Ok();
            }
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreatePaymentFor(long orderId)
        {
            var request = new GetOrderRequest
            {
                OrderId = orderId,
                CustomerEmail = _cookieAuthentication.GetAuthenticationToken()
            };
            var response = await _orderService.GetOrderAsync(request);

            if (response?.Order == null)
            {
                return NotFound();
            }

            var orderPaymentRequest = _mapper.Map<OrderView, OrderPaymentRequest>(response.Order);
            var paymentPostData = _paymentService.GeneratePostDataFor(orderPaymentRequest);

            return View("PaymentPost", paymentPostData);
        }

        public IActionResult PaymentComplete()
        {
            return View();
        }

        public IActionResult PaymentCancel()
        {
            return View();
        }
    }
}