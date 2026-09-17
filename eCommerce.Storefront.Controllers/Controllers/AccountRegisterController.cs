using System;
using System.Threading.Tasks;
using eCommerce.Storefront.Controllers.ActionArguments;
using eCommerce.Storefront.Controllers.ViewModels.Account;
using eCommerce.Storefront.Services.Interfaces;
using eCommerce.Storefront.Services.Messaging.CustomerService;
using Microsoft.AspNetCore.Mvc;
using eCommerce.Storefront.Controllers.Services.Interfaces;
using eCommerce.Storefront.Model;
using eCommerce.Storefront.Controllers.Models;
using eCommerce.Storefront.Repository.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eCommerce.Storefront.Controllers.Controllers
{
    public class AccountRegisterController(ILocalAuthenticationService authenticationService,
        ICustomerService customerService,
        ICookieAuthentication cookieAuthentication,
        IActionArguments actionArguments,
        ILogger<AccountRegisterController> logger,
        ShopDataContext shopDataContext) : BaseAccountController(authenticationService,
            customerService,
            cookieAuthentication,
            actionArguments)
    {
        private readonly ILogger<AccountRegisterController> _logger = logger;
        private readonly ShopDataContext _shopDataContext = shopDataContext;

        public IActionResult Register()
        {
            var accountView = InitializeAccountViewWithIssue(false, string.Empty);

            return View(accountView);
        }

        [HttpPost]
        public async Task<IActionResult> Register(string password, string email, string firstName, string secondName, string returnUrl)
        {
            await _shopDataContext.Database.BeginTransactionAsync();

            User user = null;

            try
            {
                user = await _authenticationService.RegisterUserAsync(email, password, true, ["Customer"]);
            }
            catch (InvalidOperationException ex)
            {
                await SafeRollbackAsync();

                var accountView = InitializeAccountViewWithIssue(true, ex.Message);

                ViewData[FormDataKeys.Email.ToString()] = email;
                ViewData[FormDataKeys.FirstName.ToString()] = firstName;
                ViewData[FormDataKeys.SecondName.ToString()] = secondName;
                ViewData["ReturnUrl"] = returnUrl;

                return View(accountView);
            }
            catch (Exception ex)
            {
                await SafeRollbackAsync();
                _logger.LogError(ex, "An error occurred while registering user with email {Email}.", email);

                // Identity user was created but customer creation will fail —
                // attempt to clean up the orphaned Identity user so the email
                // can be re-used. If this fails, we still surface the original
                // error; the orphan is a lesser evil than a 500.
                try
                {
                    await _authenticationService.DeleteUserAsync(user.Id);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogError(cleanupEx, "Failed to delete orphaned Identity user {UserId} after registration failure.", user.Id);
                }

                throw;
            }

            if (user != null && user.IsAuthenticated)
            {
                try
                {
                    await _customerService.CreateCustomerAsync(new CreateCustomerRequest
                    {
                        UserId = user.Id,
                        Email = email,
                        FirstName = firstName,
                        SecondName = secondName
                    });

                    await _shopDataContext.Database.CommitTransactionAsync();

                    // Set the auth cookie AFTER the commit succeeds so it is not
                    // left behind in the browser when the transaction is rolled back.
                    await _cookieAuthentication.SetAuthenticationTokenAsync(user.Id, user.Email, ["Customer"]);

                    // Use the returnUrl bound from the form's hidden field; on the POST the
                    // query string's ReturnUrl is gone (Html.BeginForm posts to
                    // /AccountRegister/Register), so reading from the action arguments
                    // here would always be null.
                    return RedirectBasedOn(returnUrl);
                }
                catch (EntityBaseIsInvalidException ex)
                {
                    await SafeRollbackAsync();
                    await _cookieAuthentication.SignOutAsync();

                    var accountView = InitializeAccountViewWithIssue(true, ex.Message);

                    ViewData[FormDataKeys.Email.ToString()] = email;
                    ViewData[FormDataKeys.FirstName.ToString()] = firstName;
                    ViewData[FormDataKeys.SecondName.ToString()] = secondName;
                    ViewData["ReturnUrl"] = returnUrl;

                    return View(accountView);
                }
                catch (Exception ex)
                {
                    await SafeRollbackAsync();
                    await _cookieAuthentication.SignOutAsync();
                    _logger.LogError(ex, "An error occurred while creating the customer with email {Email}.", email);

                    throw;
                }
            }
            else
            {
                await SafeRollbackAsync();

                var accountView = InitializeAccountViewWithIssue(true, "Sorry we could not authenticate you. Please try again.");

                ViewData[FormDataKeys.Email.ToString()] = email;
                ViewData[FormDataKeys.FirstName.ToString()] = firstName;
                ViewData[FormDataKeys.SecondName.ToString()] = secondName;
                ViewData["ReturnUrl"] = returnUrl;

                return View(accountView);
            }
        }

        /// <summary>
        /// Rolls back the current database transaction, tolerating the case where the
        /// transaction has already been broken by a failed commit or connection drop.
        /// </summary>
        private async Task SafeRollbackAsync()
        {
            try
            {
                await _shopDataContext.Database.RollbackTransactionAsync();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Rollback failed (transaction may already be broken).");
            }
        }

        private AccountView InitializeAccountViewWithIssue(bool hasIssue, string message)
        {
            var accountView = new AccountView();

            accountView.CallBackSettings.Action = "Register";
            accountView.CallBackSettings.Controller = "AccountRegister";
            accountView.HasIssue = hasIssue;
            accountView.Message = message;

            var returnUrl = _actionArguments.GetValueForArgument(ActionArgumentKey.ReturnUrl);

            accountView.CallBackSettings.ReturnUrl = GetReturnActionFrom(returnUrl).ToString();

            // Seed the form's hidden returnUrl field (raw URL, e.g. "/Checkout/Checkout")
            // so the POST can bind it and resume the original destination after registering.
            ViewData["ReturnUrl"] = returnUrl;

            return accountView;
        }
    }
}