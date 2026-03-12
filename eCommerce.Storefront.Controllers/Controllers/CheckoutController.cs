using System.Linq;
using eCommerce.Storefront.Controllers.ViewModels.Checkout;
using eCommerce.Storefront.Services.Interfaces;
using eCommerce.Storefront.Services.Messaging.CustomerService;
using eCommerce.Storefront.Services.Messaging.OrderService;
using eCommerce.Storefront.Services.Messaging.ProductCatalogService;
using eCommerce.Storefront.Services.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using eCommerce.Storefront.Controllers.Services.Interfaces;

namespace eCommerce.Storefront.Controllers.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CheckoutController : BaseController
    {
        private readonly IBasketService _basketService;
        private readonly IOrderService _orderService;

        public CheckoutController(IBasketService basketService,
            ICustomerService customerService,
            IOrderService orderService,
            ICookieAuthentication cookieAuthentication) : base(cookieAuthentication, 
                customerService)
        {
            _basketService = basketService;
            _orderService = orderService;
        }

        public async Task<IActionResult> Checkout()
        {
            var customerRequest = new GetCustomerRequest
            {
                CustomerEmail = _cookieAuthentication.GetAuthenticationToken()
            };
            var customerResponse = await _customerService.GetCustomerAsync(customerRequest);

            if (customerResponse.CustomerFound)
            {
                var customerView = customerResponse.Customer;

                if (customerView.DeliveryAddressBook.Any())
                {
                    var orderConfirmationView = new OrderConfirmationView();
                    var getBasketRequest = new GetBasketRequest
                    {
                        BasketId = await GetBasketIdAsync()
                    };
                    var basketResponse = await _basketService.GetBasketAsync(getBasketRequest);

                    orderConfirmationView.Basket = basketResponse.Basket;
                    orderConfirmationView.DeliveryAddresses = customerView.DeliveryAddressBook;

                    return View("ConfirmOrder", orderConfirmationView);
                }

                return AddDeliveryAddress();
            }
            else 
            {
                await _cookieAuthentication.SignOutAsync();

                return RedirectToAction("Register", "AccountRegister");
            }
        }

        public IActionResult AddDeliveryAddress()
        {
            var deliveryAddressView = new DeliveryAddressView();

            return View("AddDeliveryAddress", deliveryAddressView);
        }

        [HttpPost]
        public async Task<IActionResult> AddDeliveryAddress(DeliveryAddressView deliveryAddressView)
        {
            var request = new DeliveryAddressAddRequest
            {
                Address = deliveryAddressView,
                CustomerEmail = _cookieAuthentication.GetAuthenticationToken()
            };

            await _customerService.AddDeliveryAddressAsync(request);

            return await Checkout();
        }

        public async Task<IActionResult> PlaceOrder(IFormCollection collection)
        {
            var request = new CreateOrderRequest
            {
                BasketId = await GetBasketIdAsync(),
                CustomerEmail = _cookieAuthentication.GetAuthenticationToken(),
                DeliveryId = int.Parse(collection[FormDataKeys.DeliveryAddress.ToString()])
            };
            var response = await _orderService.CreateOrderAsync(request);

            return RedirectToAction("CreatePaymentFor", "Payment", new { orderId = response.Order.Id });
        }
    }
}