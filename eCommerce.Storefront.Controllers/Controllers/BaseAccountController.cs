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
            // The auth-challenge return URL is a raw path like "/Checkout/Checkout",
            // so match it through GetReturnActionFrom (which recognises "checkout")
            // instead of comparing to the "GoToCheckout" token — the two branches here
            // never echo the raw URL back, keeping this free of open-redirects.
            if (GetReturnActionFrom(returnUrl) == ActionArgumentKey.GoToCheckout)
            {
                return RedirectToAction("Checkout", "Checkout");
            }

            return RedirectToAction("Index", "Home");
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
