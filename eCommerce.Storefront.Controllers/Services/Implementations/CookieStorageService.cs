using System;
using eCommerce.Storefront.Controllers.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace eCommerce.Storefront.Controllers.Services.Implementations
{
    public class CookieStorageService : ICookieStorageService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CookieStorageService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void Save(string key, string value, DateTime expires)
        {
            _httpContextAccessor?.HttpContext?.Response?.Cookies?.Append(key, value, new CookieOptions
            {
                Expires = expires,
                HttpOnly = true,
                Secure = true,
                IsEssential = true,
                SameSite = SameSiteMode.Strict
            });
        }
        
        public string Retrieve(string key)
        {
            string cookie = _httpContextAccessor?.HttpContext?.Request?.Cookies?[key];
            
            if (cookie != null)
            {
                return cookie;
            }
            else
            {
                return string.Empty;
            }                           
        }
    }
}