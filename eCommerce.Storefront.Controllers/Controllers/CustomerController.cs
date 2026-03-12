using System.Linq;
using eCommerce.Storefront.Controllers.ViewModels.CustomerAccount;
using eCommerce.Storefront.Services.Interfaces;
using eCommerce.Storefront.Services.Messaging.CustomerService;
using eCommerce.Storefront.Services.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using eCommerce.Storefront.Controllers.Services.Interfaces;
using eCommerce.Storefront.Model;

namespace eCommerce.Storefront.Controllers.Controllers
{
    [Authorize(Roles = "Customer")]    
    public class CustomerController : BaseController
    {
        public CustomerController(ICustomerService customerService,
            ICookieAuthentication cookieAuthentication) : base(cookieAuthentication, customerService)
        {
        }

        public async Task<IActionResult> Detail()
        {
            var customerRequest = new GetCustomerRequest
            {
                CustomerEmail = _cookieAuthentication.GetAuthenticationToken()
            };
            var response = await _customerService.GetCustomerAsync(customerRequest);

            if (response.CustomerFound)
            {
                var customerDetailView = new CustomerDetailView
                {
                    Customer = response.Customer,
                    BasketSummary = await GetBasketSummaryViewAsync()
                };

                return View(customerDetailView);
            }
            else 
            {
                await _cookieAuthentication.SignOutAsync();

                return RedirectToAction("Register", "AccountRegister");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Detail(CustomerView customerView)
        {
            var request = new ModifyCustomerRequest
            {
                NewEmail = customerView.Email,
                FirstName = customerView.FirstName,
                SecondName = customerView.SecondName,
                CurrentEmail = _cookieAuthentication.GetAuthenticationToken()
            };
            var customerDetailView = new CustomerDetailView
            {
                BasketSummary = await GetBasketSummaryViewAsync()
            };

            try
            {       
                var response = await _customerService.ModifyCustomerAsync(request);  

                customerDetailView.Customer = response.Customer;

                await _cookieAuthentication.SetAuthenticationTokenAsync(customerDetailView.Customer.Email, new List<string> { "Customer" });
            }
            catch (EntityBaseIsInvalidException ex)
            {
                ViewData["IssueMessage"] = ex.Message;
                customerDetailView.Customer = customerView;
            }
        
            return View(customerDetailView);
        }

        public async Task<IActionResult> DeliveryAddresses()
        {
            var customerRequest = new GetCustomerRequest
            {
                CustomerEmail = _cookieAuthentication.GetAuthenticationToken()
            };
            var response = await _customerService.GetCustomerAsync(customerRequest);

            if (response.CustomerFound)
            {
                var customerDetailView = new CustomerDetailView
                {
                    Customer = response.Customer,
                    BasketSummary = await GetBasketSummaryViewAsync()
                };

                return View("DeliveryAddresses", customerDetailView);
            }
            else 
            {
                await _cookieAuthentication.SignOutAsync();

                return RedirectToAction("Register", "AccountRegister");
            }
        }

        public async Task<IActionResult> EditDeliveryAddress(int deliveryAddressId)
        {
            var customerRequest = new GetCustomerRequest
            {
                CustomerEmail = _cookieAuthentication.GetAuthenticationToken()
            };
            var response = await _customerService.GetCustomerAsync(customerRequest);

            if (response.CustomerFound)
            {
                var deliveryAddressView = new CustomerDeliveryAddressView
                {
                    CustomerView = response.Customer,
                    Address = response.Customer.DeliveryAddressBook.FirstOrDefault(d => d.Id == deliveryAddressId),
                    BasketSummary = await GetBasketSummaryViewAsync()
                };

                return View(deliveryAddressView);
            }
            else 
            {
                await _cookieAuthentication.SignOutAsync();
                
                return RedirectToAction("Register", "AccountRegister");
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditDeliveryAddress(DeliveryAddressView deliveryAddressView)
        {
            var request = new DeliveryAddressModifyRequest
            {
                Address = deliveryAddressView,
                CustomerEmail = _cookieAuthentication.GetAuthenticationToken()
            };

            await _customerService.ModifyDeliveryAddressAsync(request);

            return await DeliveryAddresses();
        }

        public async Task<IActionResult> AddDeliveryAddress()
        {
            var customerDeliveryAddressView = new CustomerDeliveryAddressView
            {
                Address = new DeliveryAddressView(),
                BasketSummary = await GetBasketSummaryViewAsync()
            };

            return View(customerDeliveryAddressView);
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

            return await DeliveryAddresses();
        }
    }
}