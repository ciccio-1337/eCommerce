using System;
using System.Threading.Tasks;
using eCommerce.Storefront.Controllers.Services.Interfaces;
using eCommerce.Storefront.Controllers.ViewModels;
using eCommerce.Storefront.Services.Interfaces;
using eCommerce.Storefront.Services.Messaging.CustomerService;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.Storefront.Controllers.Controllers
{
    public abstract class BaseController : Controller
    {
        protected readonly ICookieAuthentication _cookieAuthentication;
        protected readonly ICustomerService _customerService;
        
        protected BaseController(ICookieAuthentication cookieAuthentication,
            ICustomerService customerService)
        {
            _cookieAuthentication = cookieAuthentication;
            _customerService = customerService;
        }
        
        protected async Task<BasketSummaryView> GetBasketSummaryViewAsync()
        {
            var basketTotal = string.Empty;
            var numberOfItems = 0;
            var email = _cookieAuthentication.GetAuthenticationToken();

            if (!string.IsNullOrWhiteSpace(email))
            {
                var response = await _customerService.GetCustomerAsync(new GetCustomerRequest
                {
                    CustomerEmail = email,
                    LoadBasketSummary = true
                });

                if (response.CustomerFound && response.Basket != null)
                {
                    basketTotal = response.Basket.BasketTotal;
                    numberOfItems = response.Basket.NumberOfItems;
                }
            }

            return new BasketSummaryView
            {
                BasketTotal = basketTotal,
                NumberOfItems = numberOfItems
            };
        }
        
        protected async Task<Guid> GetBasketIdAsync()
        {
            var basketId = Guid.Empty;            
            var email = _cookieAuthentication.GetAuthenticationToken();

            if (!string.IsNullOrWhiteSpace(email))
            {
                var response = await _customerService.GetCustomerAsync(new GetCustomerRequest
                {
                    CustomerEmail = email,
                    LoadBasketSummary = true
                });

                if (response.CustomerFound && response.Basket != null)
                {
                    basketId = response.Basket.Id;
                }
            }
            
            return basketId;
        }
    }
}
