using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using eCommerce.Storefront.Controllers.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace eCommerce.Storefront.Controllers.Services.Implementations
{
    public class AspNetCoreCookieAuthentication(
        IHttpContextAccessor httpContextAccessor,
        SignInManager<IdentityUser> signInManager) : ICookieAuthentication
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly SignInManager<IdentityUser> _signInManager = signInManager;

        public async Task SetAuthenticationTokenAsync(string userId, string email, IEnumerable<string> roles)
        {
            var user = await _signInManager.UserManager.FindByIdAsync(userId);

            if (user is null)
            {
                throw new InvalidOperationException(
                    "Cannot create an authentication cookie for an unknown user.");
            }

            // Build the principal from the Identity store so it carries the current
            // roles and the security-stamp claim that the cookie validator checks.
            var principal = await _signInManager.CreateUserPrincipalAsync(user);

            await _httpContextAccessor.HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);
        }

        public string GetAuthenticationToken()
        {
            return _httpContextAccessor?.HttpContext?.User?.Identity?.Name;
        }

        public async Task SignOutAsync()
        {
            await _httpContextAccessor.HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
