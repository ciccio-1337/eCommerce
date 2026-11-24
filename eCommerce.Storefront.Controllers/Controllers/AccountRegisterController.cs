using System;
using System.Security.Cryptography;
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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace eCommerce.Storefront.Controllers.Controllers
{
    [AllowAnonymous]
    public class AccountRegisterController(ILocalAuthenticationService authenticationService,
        ICustomerService customerService,
        ICookieAuthentication cookieAuthentication,
        IActionArguments actionArguments,
        IEmailService emailService,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<AccountRegisterController> logger,
        ShopDataContext shopDataContext,
        ILookupNormalizer lookupNormalizer,
        IAccountActionThrottle accountActionThrottle) : BaseAccountController(authenticationService,
            customerService,
            cookieAuthentication,
            actionArguments)
    {
        private readonly IEmailService _emailService = emailService;
        private readonly IConfiguration _configuration = configuration;
        private readonly IWebHostEnvironment _environment = environment;
        private readonly ILogger<AccountRegisterController> _logger = logger;
        private readonly ShopDataContext _shopDataContext = shopDataContext;
        private readonly ILookupNormalizer _lookupNormalizer = lookupNormalizer;
        private readonly IAccountActionThrottle _accountActionThrottle = accountActionThrottle;

        // A resend is expensive (mail transport + token generation) and is a
        // mail-bombing primitive when the same address is targeted repeatedly.
        private static readonly TimeSpan ResendCooldown = TimeSpan.FromMinutes(1);

        public IActionResult Register()
        {
            var accountView = InitializeAccountViewWithIssue(false, string.Empty);

            return View(accountView);
        }

        [HttpPost]
        [EnableRateLimiting("registration")]
        [RequestSizeLimit(32 * 1024)]
        public async Task<IActionResult> Register(string password, string email, string firstName, string secondName, string returnUrl)
        {
            var validationMessage = CredentialInputValidator.ValidateRegistration(email, password, firstName, secondName);

            if (validationMessage != null)
            {
                return RegistrationFailed(email, firstName, secondName, returnUrl, validationMessage);
            }

            await using var transaction = await _shopDataContext.Database.BeginTransactionAsync();

            User user = null;

            try
            {
                // The account stays unconfirmed until the customer proves ownership
                // of the address by following the emailed link.
                user = await _authenticationService.RegisterUserAsync(email, password, false, ["Customer"]);
            }
            catch (AccountAlreadyExistsException)
            {
                await SafeRollbackAsync();

                // Respond exactly like a successful registration. The existing
                // address owner gets a best-effort notice; the caller learns
                // nothing about whether the account exists.
                await TryNotifyExistingAccountAsync(email);

                ViewData["Title"] = "Check your email";
                ViewData["Email"] = email;

                return View("RegistrationPending");
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

            if (!confirmed)
            {
                ViewData["Title"] = "Confirmation failed";
                ViewData["Confirmed"] = false;

                return View();
            }

            // Double opt-in: the password chosen at registration is discarded. An
            // attacker could otherwise register a victim's address with a password
            // they know and wait for the victim to click the confirmation link.
            // Only the mailbox owner can reach this point, so they set the password.
            var user = await _shopDataContext.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                ViewData["Title"] = "Confirmation failed";
                ViewData["Confirmed"] = false;

                return View();
            }

            var rotationCode = await _authenticationService.GeneratePasswordResetTokenAsync(userId);

            if (string.IsNullOrWhiteSpace(rotationCode))
            {
                ViewData["Title"] = "Confirmation failed";
                ViewData["Confirmed"] = false;

                return View();
            }

            // Guarantee the Identity password policy (upper, lower, digit, symbol)
            // so the rotation cannot fail validation and leave the attacker-chosen
            // registration password in place.
            var randomPassword = "A1a!" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            if (!await _authenticationService.ResetPasswordAsync(userId, rotationCode, randomPassword))
            {
                ViewData["Title"] = "Confirmation failed";
                ViewData["Confirmed"] = false;

                return View();
            }

            var setPasswordCode = await _authenticationService.GeneratePasswordResetTokenAsync(userId);

            if (string.IsNullOrWhiteSpace(setPasswordCode))
            {
                ViewData["Title"] = "Confirmation failed";
                ViewData["Confirmed"] = false;

                return View();
            }

            ViewData["Title"] = "Set your password";
            ViewData["Email"] = user.Email;
            ViewData["UserId"] = userId;
            ViewData["ResetToken"] = setPasswordCode;

            return View("SetPassword");
        }

        [HttpPost]
        [EnableRateLimiting("auth")]
        [RequestSizeLimit(16 * 1024)]
        public async Task<IActionResult> SetPassword(string userId, string code, string password, string confirmPassword)
        {
            if (!CredentialInputValidator.IsAcceptablePassword(password)
                || !string.Equals(password, confirmPassword, StringComparison.Ordinal))
            {
                ViewData["Title"] = "Set your password";
                ViewData["IssueMessage"] = "Choose a password of at most 128 characters and confirm it.";
                ViewData["ResetToken"] = code;
                ViewData["UserId"] = userId;

                return View("SetPassword");
            }

            var reset = await _authenticationService.ResetPasswordAsync(userId, code, password);

            if (!reset)
            {
                ViewData["Title"] = "Set your password";
                ViewData["IssueMessage"] = "The password could not be set. The link may have expired; request a new confirmation email.";
                ViewData["ResetToken"] = code;
                ViewData["UserId"] = userId;

                return View("SetPassword");
            }

            ViewData["Title"] = "Password set";
            ViewData["Confirmed"] = true;

            return View("ConfirmEmail");
        }

        [HttpPost]
        [EnableRateLimiting("registration")]
        [RequestSizeLimit(16 * 1024)]
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

            // Reuse the same normaliser Identity applied when the user was created;
            // ToUpperInvariant only matches the default implementation and silently
            // stops resolving addresses if a custom normaliser is configured.
            var normalizedEmail = _lookupNormalizer.NormalizeEmail(email) ?? string.Empty;
            var user = await _shopDataContext.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

            if (user == null || user.EmailConfirmed)
            {
                return View("RegistrationPending");
            }

            // Per-address cooldown: the IP rate limiter cannot stop a distributed
            // attacker from mail-bombing one inbox. The response stays neutral.
            if (!_accountActionThrottle.TryAcquire(normalizedEmail, ResendCooldown))
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

        /// <summary>
        /// Warns the owner of an already-registered address that somebody tried to
        /// register it. Best effort: a mail failure must not change the neutral
        /// response returned to the caller.
        /// </summary>
        private async Task TryNotifyExistingAccountAsync(string email)
        {
            try
            {
                var normalizedEmail = _lookupNormalizer.NormalizeEmail(email) ?? string.Empty;
                var user = await _shopDataContext.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

                if (user == null || string.IsNullOrWhiteSpace(user.Email))
                {
                    return;
                }

                var from = _configuration["MailSettings:Smtp:Network:UserName"]
                    ?? _configuration["MailSettingsSmtpNetworkUserName"]
                    ?? string.Empty;

                await _emailService.SendMailAsync(
                    from,
                    user.Email,
                    "Registration attempt for your address",
                    "Somebody tried to create an account with this email address. "
                    + "If it was you, sign in or reset your password. If it was not, no action is required.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send the existing-account notice.");
            }
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
