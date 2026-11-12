using System;
using System.Linq;
using eCommerce.Storefront.Controllers.ViewModels.CustomerAccount;
using eCommerce.Storefront.Services.Implementations;
using eCommerce.Storefront.Services.Interfaces;
using eCommerce.Storefront.Services.Messaging.CustomerService;
using eCommerce.Storefront.Services.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using eCommerce.Storefront.Controllers.Services.Interfaces;
using eCommerce.Storefront.Model;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace eCommerce.Storefront.Controllers.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerController(ICustomerService customerService,
        ICookieAuthentication cookieAuthentication,
        ILocalAuthenticationService authenticationService,
        IEmailService emailService,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<CustomerController> logger) : BaseController(cookieAuthentication, customerService)
    {
        private readonly ILocalAuthenticationService _authenticationService = authenticationService;
        private readonly IEmailService _emailService = emailService;
        private readonly IConfiguration _configuration = configuration;
        private readonly IWebHostEnvironment _environment = environment;
        private readonly ILogger<CustomerController> _logger = logger;
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
        public async Task<IActionResult> Detail(CustomerView customerView, string currentPassword)
        {
            var currentEmail = _cookieAuthentication.GetAuthenticationToken();
            var emailChanging = !string.IsNullOrWhiteSpace(customerView.Email)
                && !string.Equals(currentEmail, customerView.Email, StringComparison.OrdinalIgnoreCase);

            // Changing the sign-in address is an account-takeover primitive when a
            // session is stolen. Require the current password (step-up re-auth) so a
            // borrowed cookie alone cannot move the account to an attacker address.
            if (emailChanging && !await _authenticationService.ValidatePasswordAsync(currentEmail, currentPassword))
            {
                ViewData["IssueMessage"] = "Enter your current password to change your email address.";

                return View(new CustomerDetailView
                {
                    Customer = customerView,
                    BasketSummary = await GetBasketSummaryViewAsync()
                });
            }

            var request = new ModifyCustomerRequest
            {
                NewEmail = customerView.Email,
                FirstName = customerView.FirstName,
                SecondName = customerView.SecondName,
                CurrentEmail = currentEmail
            };
            var customerDetailView = new CustomerDetailView
            {
                BasketSummary = await GetBasketSummaryViewAsync()
            };

            try
            {
                var response = await _customerService.ModifyCustomerAsync(request);

                customerDetailView.Customer = response.Customer;

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId))
                {
                    await _cookieAuthentication.SignOutAsync();

                    return RedirectToAction("Register", "AccountRegister");
                }

                if (response.EmailChanged)
                {
                    // Warn the *old* address as well. If the change was made by an
                    // attacker, the legitimate owner must be able to notice and revert
                    // it; the new address only receives the confirmation link.
                    try
                    {
                        await SendEmailChangeNoticeAsync(currentEmail, response.Customer.Email);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send the email-change notice to the old address for user {UserId}.", userId);
                    }

                    try
                    {
                        await SendEmailChangeConfirmationAsync(userId, response.Customer.Email);
                    }
                    catch (Exception ex)
                    {
                        // The address is already persisted and therefore unconfirmed; the
                        // customer must still sign out and use the resend form if needed.
                        _logger.LogError(ex, "Failed to send the email-change confirmation for user {UserId}.", userId);
                        ViewData["IssueMessage"] =
                            "Your email was updated, but we could not send the confirmation message. Please resend it below.";
                    }

                    // Changing the sign-in address revokes the current session (the
                    // security stamp changed), so force a fresh login after confirmation.
                    await _cookieAuthentication.SignOutAsync();

                    ViewData["Title"] = "Confirm your new email";
                    ViewData["Email"] = response.Customer.Email;

                    return View("~/Views/AccountRegister/EmailChangePending.cshtml");
                }

                await _cookieAuthentication.SetAuthenticationTokenAsync(userId, customerDetailView.Customer.Email, ["Customer"]);
            }
            catch (EntityBaseIsInvalidException ex)
            {
                ViewData["IssueMessage"] = ex.Message;
                customerDetailView.Customer = customerView;
            }
            catch (EmailAlreadyInUseException ex)
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

        public async Task<IActionResult> EditDeliveryAddress(long deliveryAddressId)
        {
            var customerRequest = new GetCustomerRequest
            {
                CustomerEmail = _cookieAuthentication.GetAuthenticationToken()
            };
            var response = await _customerService.GetCustomerAsync(customerRequest);

            if (response.CustomerFound)
            {
                var address = response.Customer.DeliveryAddressBook.FirstOrDefault(d => d.Id == deliveryAddressId);

                if (address == null)
                {
                    return RedirectToAction("DeliveryAddresses");
                }

                var deliveryAddressView = new CustomerDeliveryAddressView
                {
                    CustomerView = response.Customer,
                    Address = address,
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

            try
            {
                await _customerService.ModifyDeliveryAddressAsync(request);
            }
            catch (CustomerNotFoundException)
            {
                await _cookieAuthentication.SignOutAsync();

                return RedirectToAction("Register", "AccountRegister");
            }
            catch (DeliveryAddressNotFoundException)
            {
                // Stale address ID — silent redirect back to list is the existing
                // GET behavior; keep it consistent.
                return RedirectToAction("DeliveryAddresses");
            }
            catch (EntityBaseIsInvalidException ex)
            {
                ViewData["IssueMessage"] = ex.Message;

                return View(new CustomerDeliveryAddressView
                {
                    Address = deliveryAddressView,
                    BasketSummary = await GetBasketSummaryViewAsync()
                });
            }

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

            try
            {
                await _customerService.AddDeliveryAddressAsync(request);
            }
            catch (CustomerNotFoundException)
            {
                await _cookieAuthentication.SignOutAsync();
                
                return RedirectToAction("Register", "AccountRegister");
            }
            catch (EntityBaseIsInvalidException ex)
            {
                ViewData["IssueMessage"] = ex.Message;

                return View(new CustomerDeliveryAddressView
                {
                    Address = deliveryAddressView,
                    BasketSummary = await GetBasketSummaryViewAsync()
                });
            }

            return await DeliveryAddresses();
        }

        private async Task SendEmailChangeNoticeAsync(string oldEmail, string newEmail)
        {
            if (string.IsNullOrWhiteSpace(oldEmail))
            {
                return;
            }

            var from = _configuration["MailSettings:Smtp:Network:UserName"]
                ?? _configuration["MailSettingsSmtpNetworkUserName"]
                ?? string.Empty;

            await _emailService.SendMailAsync(
                from,
                oldEmail,
                "Your email address was changed",
                $"The email address on your account was changed from {oldEmail} to {newEmail}. "
                + "If you did not make this change, contact support immediately: the new address must be "
                + "confirmed before the account can sign in again.");
        }

        private async Task SendEmailChangeConfirmationAsync(string userId, string email)
        {
            var code = await _authenticationService.GenerateEmailConfirmationTokenAsync(userId);

            if (string.IsNullOrWhiteSpace(code))
            {
                throw new InvalidOperationException("Could not generate an email confirmation token.");
            }

            var relativeUrl = $"/AccountRegister/ConfirmEmail?userId={Uri.EscapeDataString(userId)}&code={Uri.EscapeDataString(code)}";
            var confirmationUrl = BuildPublicUrl(relativeUrl);

            var from = _configuration["MailSettings:Smtp:Network:UserName"]
                ?? _configuration["MailSettingsSmtpNetworkUserName"]
                ?? string.Empty;

            await _emailService.SendMailAsync(
                from,
                email,
                "Confirm your new email address",
                $"You asked to change the email address on your account. Confirm it by opening this link: {confirmationUrl}");
        }

        /// <summary>
        /// Builds an absolute URL from the configured public base address instead of
        /// trusting the request Host header. Outside Development the configuration is
        /// mandatory (and validated at startup by Program.cs).
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
    }
}