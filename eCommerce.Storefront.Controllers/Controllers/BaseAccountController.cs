using System;
using eCommerce.Storefront.Controllers.ActionArguments;
using eCommerce.Storefront.Controllers.Services.Interfaces;
using eCommerce.Storefront.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.Storefront.Controllers.Controllers
{
    public abstract class BaseAccountController(ILocalAuthenticationService authenticationService,
        ICustomerService customerService,
        ICookieAuthentication cookieAuthentication,
        IActionArguments actionArguments) : Controller
    {
        protected readonly ILocalAuthenticationService _authenticationService = authenticationService;
        protected readonly ICustomerService _customerService = customerService;
        protected readonly ICookieAuthentication _cookieAuthentication = cookieAuthentication;
        protected readonly IActionArguments _actionArguments = actionArguments;

        protected IActionResult RedirectBasedOn(string returnUrl)
        {
            if (returnUrl == ActionArgumentKey.GoToCheckout.ToString())
            {
                return RedirectToAction("Checkout", "Checkout");
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        protected static ActionArgumentKey GetReturnActionFrom(string returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && returnUrl.Contains("checkout", StringComparison.OrdinalIgnoreCase))
            {
                return ActionArgumentKey.GoToCheckout;
            }
            else
            {
                return ActionArgumentKey.GoToAccount;
            }
        }
    }
}
