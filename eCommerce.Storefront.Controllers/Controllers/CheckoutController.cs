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
            GetCustomerRequest customerRequest = new GetCustomerRequest
            {
                CustomerEmail = _cookieAuthentication.GetAuthenticationToken()
            };
            GetCustomerResponse customerResponse = _customerService.GetCustomer(customerRequest);

            if (customerResponse.CustomerFound)
            {
                CustomerView customerView = customerResponse.Customer;

                if (customerView.DeliveryAddressBook.Any())
                {
                    OrderConfirmationView orderConfirmationView = new OrderConfirmationView();
                    GetBasketRequest getBasketRequest = new GetBasketRequest
                    {
                        BasketId = GetBasketId()
                    };
                    GetBasketResponse basketResponse = _basketService.GetBasket(getBasketRequest);
                    orderConfirmationView.Basket = basketResponse.Basket;
                    orderConfirmationView.DeliveryAddresses = customerView.DeliveryAddressBook;

                    return View("ConfirmOrder", orderConfirmationView);
                }

                return AddDeliveryAddress();
            }
            else 
            {
                await _cookieAuthentication.SignOut();

                return RedirectToAction("Register", "AccountRegister");
            }
        }

        public IActionResult AddDeliveryAddress()
        {
            DeliveryAddressView deliveryAddressView = new DeliveryAddressView();

            return View("AddDeliveryAddress", deliveryAddressView);
        }

        [HttpPost]
        public async Task<IActionResult> AddDeliveryAddress(DeliveryAddressView deliveryAddressView)
        {
            DeliveryAddressAddRequest request = new DeliveryAddressAddRequest
            {
                Address = deliveryAddressView,
                CustomerEmail = _cookieAuthentication.GetAuthenticationToken()
            };

            _customerService.AddDeliveryAddress(request);

            return await Checkout();
        }

        public IActionResult PlaceOrder(IFormCollection collection)
        {
            CreateOrderRequest request = new CreateOrderRequest
            {
                BasketId = GetBasketId(),
                CustomerEmail = _cookieAuthentication.GetAuthenticationToken(),
                DeliveryId = int.Parse(collection[FormDataKeys.DeliveryAddress.ToString()])
            };
            CreateOrderResponse response = _orderService.CreateOrder(request);

            return RedirectToAction("CreatePaymentFor", "Payment", new { orderId = response.Order.Id });
        }
    }
}