using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using eCommerce.Backoffice.Client.Services.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace eCommerce.Backoffice.Client.Services.Implementations
{
    public class CustomAuthStateProvider : AuthenticationStateProvider, ILoginService
    {
        private readonly IJSRuntime _javaScriptRuntime;
        private readonly HttpClient _httpClient;
        private static readonly string _tokenkey = "TOKENKEY";
        private AuthenticationState _anonymous => new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        public CustomAuthStateProvider(IJSRuntime javaScriptRuntime, HttpClient httpClient)
        {
            _javaScriptRuntime = javaScriptRuntime;
            _httpClient = httpClient;
        }

        public async override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await _javaScriptRuntime.InvokeAsync<string>("sessionStorage.getItem", _tokenkey);

            if (!TokenStatus(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;

                await _javaScriptRuntime.InvokeVoidAsync("sessionStorage.removeItem", _tokenkey);
                
                return _anonymous;
            }

            return BuildAuthenticationState(token);
        }

        public async Task Login(string token)
        {
            await _javaScriptRuntime.InvokeVoidAsync("sessionStorage.setItem", _tokenkey, token);

            var authState = BuildAuthenticationState(token);

            NotifyAuthenticationStateChanged(Task.FromResult(authState));
        }

        public async Task Logout()
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;

            await _javaScriptRuntime.InvokeVoidAsync("sessionStorage.removeItem", _tokenkey);

            NotifyAuthenticationStateChanged(Task.FromResult(_anonymous));
        }

        private AuthenticationState BuildAuthenticationState(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(ClaimsFromJwt(token), "jwt")));
        }

        private bool TokenStatus(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return false;
                }

                var jwthandler = new JwtSecurityTokenHandler();
                var jwttoken = jwthandler.ReadJwtToken(token);
                
                return jwttoken.ValidTo > DateTime.UtcNow;
            }
            catch
            {
                return false;
            }
        }

        private IEnumerable<Claim> ClaimsFromJwt(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return null;
                }

                var jwthandler = new JwtSecurityTokenHandler();
                var jwttoken = jwthandler.ReadJwtToken(token);
                
                return jwttoken.Claims;
            }
            catch
            {
                return null;
            }
        }
    }
}