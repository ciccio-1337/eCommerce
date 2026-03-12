using System.Collections.Generic;
using System.Net.Http;
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
            var token = await GetTokenAsync();

            if (string.IsNullOrWhiteSpace(token))
            {
                _httpClient.DefaultRequestHeaders.Remove("RequestVerificationToken");
                await _javaScriptRuntime.InvokeVoidAsync("sessionStorage.removeItem", _tokenkey);
                
                return _anonymous;
            }

            return BuildAuthenticationState(token);
        }

        public async Task LoginAsync(string token)
        {
            await _javaScriptRuntime.InvokeVoidAsync("sessionStorage.setItem", _tokenkey, token);

            var authState = BuildAuthenticationState(token);

            NotifyAuthenticationStateChanged(Task.FromResult(authState));
        }

        public async Task LogoutAsync()
        {
            _httpClient.DefaultRequestHeaders.Remove("RequestVerificationToken");
            await _javaScriptRuntime.InvokeVoidAsync("sessionStorage.removeItem", _tokenkey);

            NotifyAuthenticationStateChanged(Task.FromResult(_anonymous));
        }

        public async Task<string> GetTokenAsync()
        {
            return await _javaScriptRuntime.InvokeAsync<string>("sessionStorage.getItem", _tokenkey);
        }

        private AuthenticationState BuildAuthenticationState(string token)
        {
            _httpClient.DefaultRequestHeaders.Add("RequestVerificationToken", token.Substring(token.IndexOf(':') + 1));
            
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(ClaimTypes.Name, token.Substring(0, token.IndexOf(':'))),
                new Claim(ClaimTypes.Role, "Admin")
            }, "jwt")));
        }
    }
}