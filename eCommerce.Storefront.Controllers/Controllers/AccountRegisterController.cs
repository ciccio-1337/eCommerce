using System;
using System.Threading.Tasks;
using eCommerce.Storefront.Controllers.ActionArguments;
using eCommerce.Storefront.Controllers.ViewModels.Account;
using eCommerce.Storefront.Services.Interfaces;
using eCommerce.Storefront.Services.Messaging.CustomerService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using eCommerce.Storefront.Controllers.Services.Interfaces;
using eCommerce.Storefront.Model;
using eCommerce.Storefront.Controllers.Models;
using eCommerce.Storefront.Repository.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace eCommerce.Storefront.Controllers.Controllers
{
    public class AccountRegisterController(ILocalAuthenticationService authenticationService,
        ICustomerService customerService,
        ICookieAuthentication cookieAuthentication,
        IActionArguments actionArguments,
        IEmailService emailService,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<AccountRegisterController> logger,
        ShopDataContext shopDataContext) : BaseAccountController(authenticationService,
            customerService,
            cookieAuthentication,
            actionArguments)
    {
        private readonly IEmailService _emailService = emailService;
        private readonly IConfiguration _configuration = configuration;
        private readonly IWebHostEnvironment _environment = environment;
        private readonly ILogger<AccountRegisterController> _logger = logger;
        private readonly ShopDataContext _shopDataContext = shopDataContext;

        public IActionResult Register()
        {
            var accountView = InitializeAccountViewWithIssue(false, string.Empty);

            return View(accountView);
        }

        [HttpPost]
        [EnableRateLimiting("registration")]
        public async Task<IActionResult> Register(string password, string email, string firstName, string secondName, string returnUrl)
        {
            await using var transaction = await _shopDataContext.Database.BeginTransactionAsync();

            User user = null;

            try
            {
                // The account stays unconfirmed until the customer proves ownership
                // of the address by following the emailed link.
                user = await _authenticationService.RegisterUserAsync(email, password, false, ["Customer"]);
            }
            catch (InvalidOperationException ex)
            {
                await SafeRollbackAsync();

                return RegistrationFailed(email, firstName, secondName, returnUrl, ex.Message);
            }
            catch (Exception ex)
            {
                await SafeRollbackAsync();
                _logger.LogError(ex, "An error occurred while creating a new account.");

                await TryDeleteUserAsync(user);

                throw;
            }

            if (user is null)
            {
                await SafeRollbackAsync();

                return RegistrationFailed(
                    email,
                    firstName,
                    secondName,
                    returnUrl,
                    "Sorry we could not create your account. Please try again.");
            }

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
            }
            catch (EntityBaseIsInvalidException ex)
            {
                await SafeRollbackAsync();
                await TryDeleteUserAsync(user);

                return RegistrationFailed(email, firstName, secondName, returnUrl, ex.Message);
            }
            catch (Exception ex)
            {
                await SafeRollbackAsync();
                _logger.LogError(ex, "An error occurred while creating the customer profile for user {UserId}.", user.Id);

                await TryDeleteUserAsync(user);

                throw;
            }

            try
            {
                await SendConfirmationEmailAsync(user.Id, email);
            }
            catch (Exception ex)
            {
                // The account exists and is valid; a mail-transport failure must not
                // roll it back. Surface the pending state so the customer can retry
                // once the mail configuration is fixed.
                _logger.LogError(ex, "Failed to send the confirmation email for user {UserId}.", user.Id);
            }

            ViewData["Title"] = "Check your email";
            ViewData["Email"] = email;

            return View("RegistrationPending");
        }

        [HttpGet]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            var confirmed = await _authenticationService.ConfirmEmailAsync(userId, code);

            ViewData["Title"] = confirmed ? "Email confirmed" : "Confirmation failed";
            ViewData["Confirmed"] = confirmed;

            return View();
        }

        [HttpPost]
        [EnableRateLimiting("registration")]
        public async Task<IActionResult> ResendConfirmation(string email)
        {
            // The response is deliberately identical whether the address exists, is
            // already confirmed, or the mail transport fails: this endpoint must not
            // become an account-enumeration oracle.
            ViewData["Title"] = "Check your email";
            ViewData["Email"] = email;
            ViewData["ResendMessage"] =
                "If the address belongs to an account that is not confirmed yet, a new confirmation link has been sent.";

            if (string.IsNullOrWhiteSpace(email))
            {
                return View("RegistrationPending");
            }

            var normalizedEmail = email.ToUpperInvariant();
            var user = await _shopDataContext.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

            if (user == null || user.EmailConfirmed)
            {
                return View("RegistrationPending");
            }

            try
            {
                await SendConfirmationEmailAsync(user.Id, user.Email ?? email);
            }
            catch (Exception ex)
            {
                // Log only: the caller gets the neutral message above.
                _logger.LogError(ex, "Failed to resend the confirmation email for user {UserId}.", user.Id);
            }

            return View("RegistrationPending");
        }

        private async Task SendConfirmationEmailAsync(string userId, string email)
        {
            var code = await _authenticationService.GenerateEmailConfirmationTokenAsync(userId);

            if (string.IsNullOrWhiteSpace(code))
            {
                throw new InvalidOperationException("Could not generate an email confirmation token.");
            }

            var relativeUrl = Url.Action(nameof(ConfirmEmail), "AccountRegister", new
            {
                userId,
                code
            });

            var confirmationUrl = BuildPublicUrl(relativeUrl);

            var from = _configuration["MailSettings:Smtp:Network:UserName"]
                ?? _configuration["MailSettingsSmtpNetworkUserName"]
                ?? string.Empty;

            await _emailService.SendMailAsync(
                from,
                email,
                "Confirm your email address",
                $"Welcome! Please confirm your account by opening this link: {confirmationUrl}");
        }

        /// <summary>
        /// Builds an absolute URL from the configured public base address instead
        /// of trusting the request Host header. Outside Development the setting is
        /// mandatory (Program.cs also validates it at startup).
        /// </summary>
        private string BuildPublicUrl(string relativePath)
        {
            var configuredBaseUrl = _configuration["App:PublicBaseUrl"];

            if (string.IsNullOrWhiteSpace(configuredBaseUrl))
            {
                if (!_environment.IsDevelopment())
                {
                    throw new InvalidOperationException(
                        "App:PublicBaseUrl must be configured in non-development environments.");
                }

                configuredBaseUrl = $"{Request.Scheme}://{Request.Host}";
            }

            return $"{configuredBaseUrl.TrimEnd('/')}/{relativePath?.TrimStart('/')}";
        }

        private IActionResult RegistrationFailed(
            string email,
            string firstName,
            string secondName,
            string returnUrl,
            string message)
        {
            var accountView = InitializeAccountViewWithIssue(true, message);

            ViewData[FormDataKeys.Email.ToString()] = email;
            ViewData[FormDataKeys.FirstName.ToString()] = firstName;
            ViewData[FormDataKeys.SecondName.ToString()] = secondName;
            ViewData["ReturnUrl"] = returnUrl;

            return View("Register", accountView);
        }

        private async Task TryDeleteUserAsync(User user)
        {
            if (user is null || string.IsNullOrWhiteSpace(user.Id))
            {
                return;
            }

            try
            {
                await _authenticationService.DeleteUserAsync(user.Id);
            }
            catch (Exception cleanupEx)
            {
                _logger.LogError(cleanupEx, "Failed to delete orphaned Identity user {UserId}.", user.Id);
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
