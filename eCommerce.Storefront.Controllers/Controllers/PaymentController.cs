using System;
using System.Net.Http;
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
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
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
        [AllowAnonymous]
        [EnableRateLimiting("ipn")]
        public async Task<IActionResult> PaymentCallBack(IFormCollection collection)
        {
            try
            {
                return await ProcessPaymentCallBackAsync(collection);
            }
            catch (Exception ex) when (IsTransientFailure(ex))
            {
                // A transient database/network failure means we could not record a
                // payment the buyer may already have made. Acknowledge nothing: let
                // PayPal retry so the order is eventually marked as paid.
                _logger.LogError(ex, "PaymentCallBack: transient error while processing an IPN; asking PayPal to retry.");

                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            catch (Exception ex)
            {
                // Permanent failures (malformed payload, unexpected mapping error)
                // must not put PayPal into an endless retry loop. Log and acknowledge.
                _logger.LogError(ex, "PaymentCallBack: unexpected error while processing an IPN.");

                return Ok();
            }
        }

        private static bool IsTransientFailure(Exception exception)
        {
            return exception is DbUpdateException
                or DbUpdateConcurrencyException
                or TimeoutException
                or HttpRequestException
                or OperationCanceledException;
        }

        private async Task<IActionResult> ProcessPaymentCallBackAsync(IFormCollection collection)
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
                _logger.LogWarning("Payment not ok for order id {OrderId}.", orderId);

                // Always 200 OK to stop PayPal retries; the order remains unpaid.
                return Ok();
            }
        }

        // The action only reads the order and renders the PayPal form, so both a
        // direct GET (order-history "Pay" link) and the POST form on the order page
        // are valid. It must accept GET because RedirectToAction after PlaceOrder is
        // replayed by the browser as GET; a POST-only action produced a 405.
        [AcceptVerbs("GET", "POST")]
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

            if (response.Order.OrderHasBeenPaidFor)
            {
                // A paid order must never be sent to PayPal again: the second IPN
                // would be rejected as a duplicate and the customer could be charged
                // without the order recording the second payment.
                _logger.LogWarning("CreatePaymentFor: order {OrderId} has already been paid for.", orderId);

                return RedirectToAction("Detail", "Order", new { orderId });
            }

            var orderPaymentRequest = _mapper.Map<OrderView, OrderPaymentRequest>(response.Order);
            var paymentPostData = _paymentService.GeneratePostDataFor(orderPaymentRequest);

            return View("PaymentPost", paymentPostData);
        }

        [AllowAnonymous]
        public IActionResult PaymentComplete()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult PaymentCancel()
        {
            return View();
        }
    }
}