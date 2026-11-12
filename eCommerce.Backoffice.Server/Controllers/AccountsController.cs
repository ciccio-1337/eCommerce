using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using eCommerce.Backoffice.Shared.Model.Accounts;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using eCommerce.Storefront.Model.Customers;
using eCommerce.Storefront.Services.Interfaces;
using eCommerce.Backoffice.Shared.Services.Interfaces;
using eCommerce.Storefront.Repository.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace eCommerce.Backoffice.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController(IEmailService emailService,
        SignInManager<IdentityUser> signInManager,
        IConfiguration configuration,
        IEntityService<Customer, long> customerService,
        ShopDataContext shopDataContext,
        IWebHostEnvironment environment,
        ILogger<AccountsController> logger) : ControllerBase
    {
        private readonly IEmailService _emailService = emailService;
        private readonly SignInManager<IdentityUser> _signInManager = signInManager;
        private readonly IConfiguration _configuration = configuration;
        private readonly IEntityService<Customer, long> _customerService = customerService;
        private readonly ShopDataContext _shopDataContext = shopDataContext;
        private readonly IWebHostEnvironment _environment = environment;
        private readonly ILogger<AccountsController> _logger = logger;

        // Password verification does the same PBKDF2 work for unknown accounts so
        // the login/forgot-password timings do not reveal which emails exist.
        private static readonly string DummyPasswordHash =
            new PasswordHasher<IdentityUser>().HashPassword(new IdentityUser(), Guid.NewGuid().ToString());

        [HttpGet]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<UserListItem>>> GetAccounts()
        {
            return await _signInManager.UserManager.Users.AsNoTracking().Select(u => new UserListItem
            {
                Id = u.Id,
                Email = u.Email
            }).ToListAsync();
        }

        [HttpGet("me")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
        public async Task<ActionResult<CurrentUserResponse>> GetCurrentUser()
        {
            var user = await _signInManager.UserManager.GetUserAsync(User);

            if (user is null)
            {
                return Unauthorized();
            }

            var roles = await _signInManager.UserManager.GetRolesAsync(user);

            return Ok(new CurrentUserResponse
            {
                Email = user.Email ?? string.Empty,
                Roles = roles.ToList()
            });
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
        public async Task<ActionResult<RegisterResponse>> CreateAccount(RegisterRequest registerRequest)
        {
            var user = new IdentityUser
            {
                UserName = registerRequest.Email,
                Email = registerRequest.Email
            };
            var result = await _signInManager.UserManager.CreateAsync(user, registerRequest.Password);

            if (!result.Succeeded)
            {
                var registerResponse = new RegisterResponse
                {
                    IsSuccess = false,
                    EmailExist = result.Errors.FirstOrDefault(x => x.Code.Equals("DuplicateUserName")) != null,
                    Errors = result.Errors.Select(x => x.Description),
                };

                return Ok(registerResponse);
            }

            // The endpoint is administrator-only, so a newly created backoffice account
            // is an administrator by default. Without this the account could never log
            // in (Login requires the Admin role) even after confirming its email.
            var roleResult = await _signInManager.UserManager.AddToRoleAsync(user, "Admin");

            if (!roleResult.Succeeded)
            {
                await _signInManager.UserManager.DeleteAsync(user);

                return Ok(new RegisterResponse
                {
                    IsSuccess = false,
                    Errors = roleResult.Errors.Select(x => x.Description)
                });
            }

            var code = await _signInManager.UserManager.GenerateEmailConfirmationTokenAsync(user);
            var smtpPassword = _configuration["MailSettings:Smtp:Network:Password"] ?? _configuration["MailSettingsSmtpNetworkPassword"];
            var smtpUserName = _configuration["MailSettings:Smtp:Network:UserName"] ?? _configuration["MailSettingsSmtpNetworkUserName"];

            if (!string.IsNullOrWhiteSpace(smtpPassword))
            {
                var urlConfirmation = BuildPublicUrl(
                    $"admin/account/emailconfirmation/?userid={HttpUtility.UrlEncode(user.Id)}&code={HttpUtility.UrlEncode(code)}");

                try
                {
                    await _emailService.SendMailAsync(smtpUserName, user.Email, "Email confirmation", $"Please confirm your account by <a href='{urlConfirmation}'>clicking here</a>", isBodyHtml: true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send the confirmation email for user {UserId}.", user.Id);
                }
            }
            else
            {
                result = await _signInManager.UserManager.ConfirmEmailAsync(user, code);

                if (!result.Succeeded)
                {
                    return Ok(new RegisterResponse
                    {
                        IsSuccess = false,
                        Errors = result.Errors.Select(x => x.Description)
                    });
                }

                return Ok(new RegisterResponse
                {
                    IsSuccess = true,
                    EmailConfirmed = true
                });
            }

            return Ok(new RegisterResponse
            {
                IsSuccess = true
            });
        }

        [HttpPut("{id}/[action]")]
        [EnableRateLimiting("auth")]
        [AllowAnonymous]
        public async Task<IActionResult> EmailConfirmation(string id, EmailConfirmationRequest confirmationRequest)
        {
            if (id != confirmationRequest.UserId)
            {
                return BadRequest();
            }

            var user = await _signInManager.UserManager.FindByIdAsync(confirmationRequest.UserId);

            if (user == null)
            {
                // Neutral response: never reveal whether a user id exists (the userId is an
                // opaque value delivered via the confirmation link, not a queryable identifier).
                return Ok(false);
            }

            try
            {
                // The confirmation code is an Identity-generated, signed and time-limited token,
                // so a malformed/expired code fails validation here without mutating any state.
                var result = await _signInManager.UserManager.ConfirmEmailAsync(user, confirmationRequest.Code);

                return Ok(result.Succeeded);
            }
            catch (DbUpdateConcurrencyException) when (!_signInManager.UserManager.Users.AsNoTracking().Any(u => u.Id == id))
            {
                return Ok(false);
            }
        }

        [HttpPost("forgotpassword")]
        [EnableRateLimiting("auth")]
        [AllowAnonymous]
        public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword(ForgotPasswordRequest forgotPasswordRequest)
        {
            // Always return the same response, whether the account exists, is
            // confirmed, or the mail transport fails. This prevents enumeration.
            var response = new ForgotPasswordResponse { IsSuccess = true };
            var user = await _signInManager.UserManager.FindByEmailAsync(forgotPasswordRequest.Email);

            if (user == null)
            {
                VerifyDummyPassword(forgotPasswordRequest.Email);
            }

            // This is the backoffice reset flow. A storefront customer must not be
            // able to obtain an admin-branded reset link (and the admin reset token
            // must not be minted for a non-admin account at all).
            if (user != null
                && await _signInManager.UserManager.IsEmailConfirmedAsync(user)
                && await _signInManager.UserManager.IsInRoleAsync(user, "Admin"))
            {
                var smtpUserName = _configuration["MailSettings:Smtp:Network:UserName"] ?? _configuration["MailSettingsSmtpNetworkUserName"];
                var code = await _signInManager.UserManager.GeneratePasswordResetTokenAsync(user);
                var urlConfirmation = BuildPublicUrl(
                    $"admin/account/changepassword/?code={HttpUtility.UrlEncode(code)}");

                try
                {
                    await _emailService.SendMailAsync(smtpUserName, user.Email, "Reset password", $"Please reset your password by <a href='{urlConfirmation}'>clicking here</a>", isBodyHtml: true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send the password reset email for user {UserId}.", user.Id);
                }
            }

            return Ok(response);
        }

        [HttpPost("login")]
        [EnableRateLimiting("auth")]
        // Without this the global fallback policy requires an authenticated user
        // and the endpoint answers 401 before the credentials are even read.
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> Login(LoginRequest loginRequest)
        {
            var response = new LoginResponse();
            var user = await _signInManager.UserManager.FindByEmailAsync(loginRequest.Email);

            if (user == null)
            {
                VerifyDummyPassword(loginRequest.Password);
                response.Errors = ["Username and password are invalid."];

                return Ok(response);
            }

            if (!(await _signInManager.CheckPasswordSignInAsync(user, loginRequest.Password, true)).Succeeded)
            {
                response.Errors = ["Username and password are invalid."];

                return Ok(response);
            }

            // Never issue a password-only admin session for an account that has a
            // second factor enabled: CheckPasswordSignInAsync does not challenge it.
            if (await _signInManager.UserManager.GetTwoFactorEnabledAsync(user))
            {
                _logger.LogWarning("Backoffice login refused for {UserId}: two-factor authentication is enabled but no challenge flow is configured.", user.Id);

                response.Errors = ["Username and password are invalid."];

                return Ok(response);
            }

            var roles = await _signInManager.UserManager.GetRolesAsync(user);

            // Return the same generic message as the invalid-credentials branch so the
            // response does not reveal whether the account exists or whether it is an
            // admin account (information disclosure / account enumeration).
            if (!roles.Contains("Admin"))
            {
                response.Errors = ["Username and password are invalid."];

                return Ok(response);
            }

            // The Identity principal carries the security-stamp claim and the current
            // roles, so the cookie validator can revoke the session after a password
            // change, role removal, or account deletion.
            var principal = await _signInManager.CreateUserPrincipalAsync(user);

            await HttpContext.SignInAsync(JwtBearerDefaults.AuthenticationScheme, principal);

            response.Email = user.Email ?? loginRequest.Email;
            response.IsSuccess = true;

            return Ok(response);
        }

        [HttpPost("logout")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
        public async Task<ActionResult> Logout()
        {
            await HttpContext?.SignOutAsync(JwtBearerDefaults.AuthenticationScheme);

            return Ok();
        }

        [HttpPut("[action]")]
        [EnableRateLimiting("auth")]
        // Reached from the emailed reset link, before the user is signed in.
        [AllowAnonymous]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest changePasswordRequest)
        {
            const string invalidLinkMessage = "The password reset link is invalid or has expired.";

            var user = await _signInManager.UserManager.FindByEmailAsync(changePasswordRequest.Email);

            if (user == null)
            {
                // Generic failure instead of 404 so the response never reveals which
                // emails are registered (email enumeration).
                return Ok(new ChangePasswordResponse
                {
                    IsSuccess = false,
                    Errors = [invalidLinkMessage]
                });
            }

            try
            {
                var result = await _signInManager.UserManager.ResetPasswordAsync(user, changePasswordRequest.Code, changePasswordRequest.Password);

                if (result.Succeeded)
                {
                    return Ok(new ChangePasswordResponse { IsSuccess = true });
                }

                // An invalid or expired token must be indistinguishable from an
                // unknown account; only genuine password-policy errors are surfaced.
                if (result.Errors.Any(error => error.Code == "InvalidToken"))
                {
                    return Ok(new ChangePasswordResponse
                    {
                        IsSuccess = false,
                        Errors = [invalidLinkMessage]
                    });
                }

                return Ok(new ChangePasswordResponse
                {
                    IsSuccess = false,
                    Errors = result.Errors.Select(x => x.Description)
                });
            }
            catch (DbUpdateConcurrencyException) when (!_signInManager.UserManager.Users.AsNoTracking().Any(u => u.Email == changePasswordRequest.Email))
            {
                return Ok(new ChangePasswordResponse
                {
                    IsSuccess = false,
                    Errors = [invalidLinkMessage]
                });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
        public async Task<IActionResult> DeleteAccount(string id)
        {
            var user = await _signInManager.UserManager.Users.FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            if (await _signInManager.UserManager.IsInRoleAsync(user, "Admin"))
            {
                var administrators = await _signInManager.UserManager.GetUsersInRoleAsync("Admin");

                if (administrators.Count <= 1)
                {
                    return BadRequest("The last administrator account cannot be deleted.");
                }
            }

            await using var transaction = await _shopDataContext.Database.BeginTransactionAsync();

            try
            {
                var deleteResult = await _signInManager.UserManager.DeleteAsync(user);

                if (!deleteResult.Succeeded)
                {
                    // Do not delete the Customer profile and claim success when the
                    // Identity user could not be removed: that leaves an orphaned user.
                    await transaction.RollbackAsync();

                    return BadRequest(deleteResult.Errors.Select(x => x.Description));
                }

                var customers = _customerService.Get(c => c.UserId.Equals(id));

                if (customers?.Count() > 0)
                {
                    foreach (var customer in customers)
                    {
                        await _customerService.DeleteAsync(customer.Id);
                    }
                }

                await transaction.CommitAsync();

                return NoContent();
            }
            catch
            {
                await transaction.RollbackAsync();

                throw;
            }
        }

        /// <summary>
        /// Builds an absolute URL from the configured public base address. Never
        /// trusts the request Host header for links that carry security tokens.
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

            return $"{configuredBaseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";
        }

        private void VerifyDummyPassword(string password)
        {
            _signInManager.UserManager.PasswordHasher.VerifyHashedPassword(
                new IdentityUser(),
                DummyPasswordHash,
                password ?? string.Empty);
        }
    }
}