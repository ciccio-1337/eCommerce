using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using eCommerce.Backoffice.Shared.Model.Accounts;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using eCommerce.Storefront.Model.Customers;
using eCommerce.Storefront.Services.Interfaces;
using eCommerce.Backoffice.Shared.Services.Interfaces;
using eCommerce.Storefront.Repository.EntityFrameworkCore;

namespace eCommerce.Backoffice.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [IgnoreAntiforgeryToken]
    public class AccountsController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IEntityService<Customer, long> _customerService;
        private readonly ShopDataContext _shopDataContext;

        public AccountsController(UserManager<IdentityUser> userManager,
            IEmailService emailService,
            SignInManager<IdentityUser> signInManager,
            IConfiguration configuration,
            IEntityService<Customer, long> customerService,
            ShopDataContext shopDataContext)
        {
            _userManager = userManager;
            _emailService = emailService;
            _signInManager = signInManager;
            _configuration = configuration;
            _customerService = customerService;
            _shopDataContext = shopDataContext;
        }

        [HttpGet]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<RegisterRequest>>> GetAccounts()
        {
            return await _userManager.Users.AsNoTracking().Select(u => new RegisterRequest 
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
            var result = await _userManager.CreateAsync(user, registerRequest.Password);

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

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            if (!string.IsNullOrWhiteSpace(_configuration["MailSettingsSmtpNetworkPassword"]))
            {
                var urlConfirmation = $"{Request.Scheme}://{Request.Host}/account/emailconfirmation/?userid={HttpUtility.UrlEncode(user.Id)}&code={HttpUtility.UrlEncode(code)}";

                _ = _emailService.SendMailAsync(_configuration["MailSettingsSmtpNetworkUserName"], user.Email, "Email confirmation", $"Please confirm your account by <a href='{urlConfirmation}'>clicking here</a>");
            }
            else 
            {
                result = await _userManager.ConfirmEmailAsync(user, code);

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
        public async Task<IActionResult> EmailConfirmation(string id, EmailConfirmationRequest confirmationRequest)
        {
            if (id != confirmationRequest.UserId)
            {
                return BadRequest();
            }

            var user = await _userManager.FindByIdAsync(confirmationRequest.UserId);

            if (user == null)
            {
                return NotFound();
            }

            try
            {
                var result = await _userManager.ConfirmEmailAsync(user, confirmationRequest.Code);

                return Ok(result.Succeeded);
            }
            catch (DbUpdateConcurrencyException) when (!_userManager.Users.AsNoTracking().Any(u => u.Id == id))
            {
                return NotFound();
            }
        }

        [HttpPost("forgotpassword")]
        public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword(ForgotPasswordRequest forgotPasswordRequest)
        {
            var response = new ForgotPasswordResponse();
            var user = await _userManager.FindByEmailAsync(forgotPasswordRequest.Email);

            if (user == null)
            {
                return NotFound();
            }

            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                response.Errors = new List<string> { "Not confirmed email" };
            }
            else
            {
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                var urlConfirmation = $"{Request.Scheme}://{Request.Host}/account/changepassword/?code={HttpUtility.UrlEncode(code)}";

                _ = _emailService.SendMailAsync(_configuration["MailSettingsSmtpNetworkUserName"], user.Email, "Reset password", $"Please reset your password by <a href='{urlConfirmation}'>clicking here</a>");

                response.IsSuccess = true;
            }

            return Ok(response);
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(LoginRequest loginRequest)
        {
            var response = new LoginResponse();
            var result = await _signInManager.PasswordSignInAsync(loginRequest.Email, loginRequest.Password, false, false);

            if (!result.Succeeded)
            {
                response.Errors = new List<string> { "Username and password are invalid." };

                return Ok(response);
            }

            var user = await _signInManager.UserManager.FindByEmailAsync(loginRequest.Email);

            if (user == null)
            {
                return NotFound();
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

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecurityKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.Now.AddDays(Convert.ToInt32(_configuration["Jwt:ExpiryInDays"]));
            var token = new JwtSecurityToken(_configuration["Jwt:Issuer"], _configuration["Jwt:Audience"], claims, expires: expiry, signingCredentials: creds);
            
            response.Token = new JwtSecurityTokenHandler().WriteToken(token);
            response.IsSuccess = true;
            
            return Ok(response);
        }

        [HttpPut("[action]")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest changePasswordRequest)
        {
            var user = await _userManager.FindByEmailAsync(changePasswordRequest.Email);

            if (user == null)
            {
                return NotFound();
            }

            try
            {
                var result = await _userManager.ResetPasswordAsync(user, changePasswordRequest.Code, changePasswordRequest.Password);
                var changePasswordResponse = new ChangePasswordResponse
                {
                    IsSuccess = result.Succeeded,
                    Errors = result.Errors.Select(x => x.Description)
                };

                return Ok(changePasswordResponse);
            }
            catch (DbUpdateConcurrencyException) when (!_userManager.Users.AsNoTracking().Any(u => u.Email == changePasswordRequest.Email))
            {
                return NotFound();
            }
        }

        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
        public async Task<IActionResult> DeleteAccount(string id)
        {
            await _shopDataContext.Database.BeginTransactionAsync();

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            await _userManager.DeleteAsync(user);
            
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