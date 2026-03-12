using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using eCommerce.Storefront.Controllers.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace eCommerce.Storefront.Controllers.Services.Implementations
{
    public class AspNetCoreCookieAuthentication : ICookieAuthentication
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AspNetCoreCookieAuthentication(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task SetAuthenticationTokenAsync(string email, IEnumerable<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, email)
            };

            if (roles?.Count() > 0)
            {
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await _httpContextAccessor?.HttpContext?.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
        }

        public string GetAuthenticationToken()
        {
            return _httpContextAccessor?.HttpContext?.User?.Identity?.Name;
        }

        public async Task SignOutAsync()
        {
            await _httpContextAccessor?.HttpContext?.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}