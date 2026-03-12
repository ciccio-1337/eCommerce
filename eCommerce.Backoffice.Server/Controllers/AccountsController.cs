using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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
using Microsoft.AspNetCore.Antiforgery;

namespace eCommerce.Backoffice.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [IgnoreAntiforgeryToken]
    public class AccountsController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IEntityService<Customer, long> _customerService;
        private readonly ShopDataContext _shopDataContext;
        private readonly IAntiforgery _antiforgery;

        public AccountsController(IEmailService emailService,
            SignInManager<IdentityUser> signInManager,
            IConfiguration configuration,
            IEntityService<Customer, long> customerService,
            ShopDataContext shopDataContext,
            IAntiforgery antiforgery)
        {
            _emailService = emailService;
            _signInManager = signInManager;
            _configuration = configuration;
            _customerService = customerService;
            _shopDataContext = shopDataContext;
            _antiforgery = antiforgery;
        }

        [HttpGet]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<RegisterRequest>>> GetAccounts()
        {
            return await _signInManager.UserManager.Users.AsNoTracking().Select(u => new RegisterRequest 
            { 
                Id = u.Id, 
                Email = u.Email, 
                Password = u.PasswordHash, 
                ConfirmPassword = u.PasswordHash 
            }).ToListAsync();
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

            var code = await _signInManager.UserManager.GenerateEmailConfirmationTokenAsync(user);

            if (!string.IsNullOrWhiteSpace(_configuration["MailSettingsSmtpNetworkPassword"]))
            {
                var urlConfirmation = $"{Request.Scheme}://{Request.Host}/account/emailconfirmation/?userid={HttpUtility.UrlEncode(user.Id)}&code={HttpUtility.UrlEncode(code)}";

                _ = _emailService.SendMailAsync(_configuration["MailSettingsSmtpNetworkUserName"], user.Email, "Email confirmation", $"Please confirm your account by <a href='{urlConfirmation}'>clicking here</a>");
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
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> EmailConfirmation(string id, EmailConfirmationRequest confirmationRequest)
        {
            if (id != confirmationRequest.UserId)
            {
                return BadRequest();
            }

            var user = await _signInManager.UserManager.FindByIdAsync(confirmationRequest.UserId);

            if (user == null)
            {
                return NotFound();
            }

            try
            {
                var result = await _signInManager.UserManager.ConfirmEmailAsync(user, confirmationRequest.Code);

                return Ok(result.Succeeded);
            }
            catch (DbUpdateConcurrencyException) when (!_signInManager.UserManager.Users.AsNoTracking().Any(u => u.Id == id))
            {
                return NotFound();
            }
        }

        [HttpPost("forgotpassword")]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword(ForgotPasswordRequest forgotPasswordRequest)
        {
            var response = new ForgotPasswordResponse();
            var user = await _signInManager.UserManager.FindByEmailAsync(forgotPasswordRequest.Email);

            if (user == null)
            {
                return NotFound();
            }

            if (!await _signInManager.UserManager.IsEmailConfirmedAsync(user))
            {
                response.Errors = new List<string> { "Not confirmed email" };
            }
            else
            {
                var code = await _signInManager.UserManager.GeneratePasswordResetTokenAsync(user);
                var urlConfirmation = $"{Request.Scheme}://{Request.Host}/account/changepassword/?code={HttpUtility.UrlEncode(code)}";

                _ = _emailService.SendMailAsync(_configuration["MailSettingsSmtpNetworkUserName"], user.Email, "Reset password", $"Please reset your password by <a href='{urlConfirmation}'>clicking here</a>");

                response.IsSuccess = true;
            }

            return Ok(response);
        }

        [HttpPost("login")]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult<LoginResponse>> Login(LoginRequest loginRequest)
        {
            var response = new LoginResponse();
            var user = await _signInManager.UserManager.FindByEmailAsync(loginRequest.Email);

            if (user == null || !(await _signInManager.CheckPasswordSignInAsync(user, loginRequest.Password, false)).Succeeded)
            {
                response.Errors = new List<string> { "Username and password are invalid." };

                return Ok(response);
            }

            var roles = await _signInManager.UserManager.GetRolesAsync(user);

            if (!roles.Contains("Admin"))
            {
                response.Errors = new List<string> { $"{loginRequest.Email} is not an Admin user." };

                return Ok(response);
            }
                        
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, loginRequest.Email)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var claimsIdentity = new ClaimsIdentity(claims, JwtBearerDefaults.AuthenticationScheme);

            await HttpContext?.SignInAsync(JwtBearerDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
            
            response.Token = $"{loginRequest.Email}:{_antiforgery.GetAndStoreTokens(HttpContext).RequestToken}";
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
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest changePasswordRequest)
        {
            var user = await _signInManager.UserManager.FindByEmailAsync(changePasswordRequest.Email);

            if (user == null)
            {
                return NotFound();
            }

            try
            {
                var result = await _signInManager.UserManager.ResetPasswordAsync(user, changePasswordRequest.Code, changePasswordRequest.Password);
                var changePasswordResponse = new ChangePasswordResponse
                {
                    IsSuccess = result.Succeeded,
                    Errors = result.Errors.Select(x => x.Description)
                };

                return Ok(changePasswordResponse);
            }
            catch (DbUpdateConcurrencyException) when (!_signInManager.UserManager.Users.AsNoTracking().Any(u => u.Email == changePasswordRequest.Email))
            {
                return NotFound();
            }
        }

        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
        public async Task<IActionResult> DeleteAccount(string id)
        {
            await _shopDataContext.Database.BeginTransactionAsync();

            var user = await _signInManager.UserManager.Users.FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            await _signInManager.UserManager.DeleteAsync(user);
            
            var customers = _customerService.Get(c => c.UserId.Equals(id));

            if (customers?.Count() > 0) 
            {
                foreach (var customer in customers)
                {
                    await _customerService.DeleteAsync(customer.Id);
                }
            }

            await _shopDataContext.Database.CommitTransactionAsync();

            return NoContent();
        }
    }
}