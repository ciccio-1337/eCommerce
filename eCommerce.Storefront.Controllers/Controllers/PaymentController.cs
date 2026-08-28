using System.Threading.Tasks;
using MapsterMapper;
using eCommerce.Storefront.Services.Interfaces;
using eCommerce.Storefront.Services.Messaging.OrderService;
using eCommerce.Storefront.Services.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using eCommerce.Storefront.Controllers.Services.Interfaces;
using Microsoft.Extensions.Logging;
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
        public async Task PaymentCallBack(IFormCollection collection)
        {
            var orderId = _paymentService.GetOrderIdFor(collection);
            var request = new GetOrderRequest
            {
                OrderId = orderId
            };
            var response = await _orderService.GetOrderAsync(request);

            if (response?.Order == null)
            {
                _logger.LogError("PaymentCallBack: Order {OrderId} could not be retrieved.", orderId);

                return;
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
                    OrderId = orderId
                };

                await _orderService.SetOrderPaymentAsync(paymentRequest);
            }
            else
            {
                _logger.LogWarning("Payment not ok for order id {OrderId}, payment token {PaymentToken}", orderId, transactionResult.PaymentToken);
            }
        }

        public async Task<IActionResult> CreatePaymentFor(int orderId)
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