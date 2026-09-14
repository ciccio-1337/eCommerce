using System;
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
    public class CheckoutController(IBasketService basketService,
        ICustomerService customerService,
        IOrderService orderService,
        ICookieAuthentication cookieAuthentication) : BaseController(cookieAuthentication,
            customerService)
    {
        private readonly IBasketService _basketService = basketService;
        private readonly IOrderService _orderService = orderService;

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

        [HttpPost]
        public async Task<IActionResult> PlaceOrder(IFormCollection collection)
        {
            if (!long.TryParse(collection[FormDataKeys.DeliveryAddress.ToString()], out var deliveryId))
            {
                return BadRequest("Invalid delivery address.");
            }

            var basketId = await GetBasketIdAsync();

            if (basketId == Guid.Empty)
            {
                return RedirectToAction("Detail", "Basket");
            }

            var basketResponse = await _basketService.GetBasketAsync(new GetBasketRequest
            {
                BasketId = basketId
            });

            if (basketResponse.Basket == null || !basketResponse.Basket.Items.Any())
            {
                return RedirectToAction("Detail", "Basket");
            }

            var request = new CreateOrderRequest
            {
                BasketId = basketId,
                CustomerEmail = _cookieAuthentication.GetAuthenticationToken(),
                DeliveryId = deliveryId
            };
            var response = await _orderService.CreateOrderAsync(request);

            if (response.Order == null)
            {
                return BadRequest("Order could not be created.");
            }

            return RedirectToAction("CreatePaymentFor", "Payment", new { orderId = response.Order.Id });
        }
    }
}