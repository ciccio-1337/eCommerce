using System.Threading.Tasks;
using eCommerce.Storefront.Controllers.ActionArguments;
using eCommerce.Storefront.Controllers.ViewModels.Account;
using eCommerce.Storefront.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using eCommerce.Storefront.Controllers.Services.Interfaces;

namespace eCommerce.Storefront.Controllers.Controllers
{
    public class AccountLogOnController(ILocalAuthenticationService authenticationService,
        ICustomerService customerService,
        ICookieAuthentication cookieAuthentication,
        IActionArguments actionArguments) : BaseAccountController(authenticationService,
            customerService,
            cookieAuthentication,
            actionArguments)
    {

        public IActionResult LogOn()
        {
            var accountView = InitializeAccountViewWithIssue(false, string.Empty);

            return View(accountView);
        }

        [HttpPost]
        public async Task<IActionResult> LogOn(string email, string password, string returnUrl)
        {
            try
            {
                var user = await _authenticationService.LoginAsync(email, password);

                if (user.IsAuthenticated && user.Roles.Any(r => r.Equals("Customer")))
                {
                    await _cookieAuthentication.SetAuthenticationTokenAsync(user.Id, user.Email, ["Customer"]);

                    return RedirectBasedOn(returnUrl);
                }
                else
                {
                    var accountView = InitializeAccountViewWithIssue(true, "Sorry we could not log you in. Please try again.");

                    accountView.CallBackSettings.ReturnUrl = GetReturnActionFrom(returnUrl).ToString();

                    ViewData["email"] = email;

                    return View(accountView);
                }
            }
            catch (InvalidOperationException ex)
            {
                var accountView = InitializeAccountViewWithIssue(true, ex.Message);

                accountView.CallBackSettings.ReturnUrl = GetReturnActionFrom(returnUrl).ToString();

                // Preserve the auth-challenge return URL across a failed attempt so the
                // form's hidden field still carries it on the retry.
                ViewData["email"] = email;
                ViewData["ReturnUrl"] = returnUrl;

                return View(accountView);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> LogOff()
        {
            await _cookieAuthentication.SignOutAsync();

            return RedirectToAction("Index", "Home");
        }

        private AccountView InitializeAccountViewWithIssue(bool hasIssue, string message)
        {
            var accountView = new AccountView();

            accountView.CallBackSettings.Action = "LogOn";
            accountView.CallBackSettings.Controller = "AccountLogOn";
            accountView.HasIssue = hasIssue;
            accountView.Message = message;

            var returnUrl = _actionArguments.GetValueForArgument(ActionArgumentKey.ReturnUrl);

            accountView.CallBackSettings.ReturnUrl = GetReturnActionFrom(returnUrl).ToString();

            // Seed the form's hidden returnUrl field (raw URL, e.g. "/Checkout/Checkout")
            // so the POST can bind it and resume the original destination after login.
            ViewData["ReturnUrl"] = returnUrl;

            return accountView;
        }
    }
}