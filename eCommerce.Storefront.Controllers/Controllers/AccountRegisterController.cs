using System;
using System.Threading.Tasks;
using eCommerce.Storefront.Controllers.ActionArguments;
using eCommerce.Storefront.Controllers.ViewModels.Account;
using eCommerce.Storefront.Services.Interfaces;
using eCommerce.Storefront.Services.Messaging.CustomerService;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using eCommerce.Storefront.Controllers.Services.Interfaces;
using eCommerce.Storefront.Model;
using eCommerce.Storefront.Controllers.Models;
using eCommerce.Storefront.Repository.EntityFrameworkCore;

namespace eCommerce.Storefront.Controllers.Controllers
{
    public class AccountRegisterController : BaseAccountController
    {
        private readonly ShopDataContext _shopDataContext;

        public AccountRegisterController(ILocalAuthenticationService authenticationService,
                                         ICustomerService customerService,
                                         ICookieAuthentication cookieAuthentication,
                                         IActionArguments actionArguments,
                                         ShopDataContext shopDataContext) : base(authenticationService, 
                                                                                 customerService,
                                                                                 cookieAuthentication, 
                                                                                 actionArguments)
        {
            _shopDataContext = shopDataContext;
        }

        public IActionResult Register()
        {
            var accountView = InitializeAccountViewWithIssue(false, string.Empty);

            return View(accountView);
        }

        [HttpPost]
        public async Task<IActionResult> Register(string password, string email, string firstName, string secondName)
        {
            await _shopDataContext.Database.BeginTransactionAsync();

            User user;

            try
            {
                user = await _authenticationService.RegisterUser(email, password, true, new List<string> { "Customer" });
            }
            catch (InvalidOperationException ex)
            {
                await _shopDataContext.Database.RollbackTransactionAsync();

                var accountView = InitializeAccountViewWithIssue(true, ex.Message);
                
                ViewData[FormDataKeys.Password.ToString()] = password;
                ViewData[FormDataKeys.Email.ToString()] = email;
                ViewData[FormDataKeys.FirstName.ToString()] = firstName;
                ViewData[FormDataKeys.SecondName.ToString()] = secondName;

                return View(accountView);
            }

            if (user.IsAuthenticated)
            {
                try
                {
                    _customerService.CreateCustomer(new CreateCustomerRequest
                    {
                        UserId = user.Id,
                        Email = email,
                        FirstName = firstName,
                        SecondName = secondName
                    });

                    await _cookieAuthentication.SetAuthenticationToken(user.Email, new List<string> { "Customer" });
                    await _shopDataContext.Database.CommitTransactionAsync();

                    return RedirectToAction("Detail", "Customer");
                }
                catch (EntityBaseIsInvalidException ex)
                {
                    await _shopDataContext.Database.RollbackTransactionAsync();

                    var accountView = InitializeAccountViewWithIssue(true, ex.Message);

                    ViewData[FormDataKeys.Password.ToString()] = password;
                    ViewData[FormDataKeys.Email.ToString()] = email;
                    ViewData[FormDataKeys.FirstName.ToString()] = firstName;
                    ViewData[FormDataKeys.SecondName.ToString()] = secondName;

                    return View(accountView);
                }
            }
            else
            {
                await _shopDataContext.Database.RollbackTransactionAsync();

                var accountView = InitializeAccountViewWithIssue(true, "Sorry we could not authenticate you. Please try again.");

                ViewData[FormDataKeys.Password.ToString()] = password;
                ViewData[FormDataKeys.Email.ToString()] = email;
                ViewData[FormDataKeys.FirstName.ToString()] = firstName;
                ViewData[FormDataKeys.SecondName.ToString()] = secondName;

                return View(accountView);
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

            return accountView;
        }
    }
}