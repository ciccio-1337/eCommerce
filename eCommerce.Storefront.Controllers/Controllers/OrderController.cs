using eCommerce.Storefront.Controllers.ViewModels.CustomerAccount;
using eCommerce.Storefront.Services.Interfaces;
using eCommerce.Storefront.Services.Messaging.CustomerService;
using eCommerce.Storefront.Services.Messaging.OrderService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using eCommerce.Storefront.Controllers.Services.Interfaces;

namespace eCommerce.Storefront.Controllers.Controllers
{
    [Authorize(Roles = "Customer")]
    public class OrderController : BaseController
    {
        private readonly IOrderService _orderService;

        public OrderController(ICustomerService customerService,
            IOrderService orderService,
            ICookieAuthentication cookieAuthentication) : base(cookieAuthentication,
                customerService)
        {
            _orderService = orderService;
        }

        public async Task<IActionResult> List()
        {
            var request = new GetCustomerRequest
            {
                CustomerEmail = _cookieAuthentication.GetAuthenticationToken(),
                LoadOrderSummary = true
            };
            var response = await _customerService.GetCustomerAsync(request);

            if (response.CustomerFound)
            {
                var customersOrderSummaryView = new CustomersOrderSummaryView
                {
                    Orders = response.Orders,
                    BasketSummary = await GetBasketSummaryViewAsync()
                };

                return View(customersOrderSummaryView);
            }
            else 
            {
                await _cookieAuthentication.SignOutAsync();
                
                return RedirectToAction("Register", "AccountRegister");
            }
        }
        
        public async Task<IActionResult> Detail(int orderId)
        {
            var request = new GetOrderRequest() { OrderId = orderId };
            var response = await _orderService.GetOrderAsync(request);
            var orderView = new CustomerOrderView
            {
                BasketSummary = await GetBasketSummaryViewAsync(),
                Order = response.Order
            };

            return View(orderView);
        }
    }
}