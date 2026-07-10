using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using eCommerce.Backoffice.Client.Services.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace eCommerce.Backoffice.Client.Services.Implementations
{
    public class CustomAuthStateProvider : AuthenticationStateProvider, ILoginService
    {
        private readonly IJSRuntime _javaScriptRuntime;
        private static readonly string _authenticationStateKey = "AuthenticationState";
        private AuthenticationState _anonymous => new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        public CustomAuthStateProvider(IJSRuntime javaScriptRuntime)
        {
            _javaScriptRuntime = javaScriptRuntime;
        }

        public async override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var authState = await GetAuthStateAsync();

            if (string.IsNullOrWhiteSpace(authState))
            {
                await _javaScriptRuntime.InvokeVoidAsync("sessionStorage.removeItem", _authenticationStateKey);
                
                return _anonymous;
            }

            return BuildAuthenticationState(Encoding.UTF8.GetString(Convert.FromBase64String(authState)));
        }

        public async Task LoginAsync(string email)
        {
            await _javaScriptRuntime.InvokeVoidAsync("sessionStorage.setItem", _authenticationStateKey, Convert.ToBase64String(Encoding.UTF8.GetBytes(email)));
            
            var authState = BuildAuthenticationState(email);

            NotifyAuthenticationStateChanged(Task.FromResult(authState));
        }

        public async Task LogoutAsync()
        {
            await _javaScriptRuntime.InvokeVoidAsync("sessionStorage.removeItem", _authenticationStateKey);

            NotifyAuthenticationStateChanged(Task.FromResult(_anonymous));
        }

        public async Task<string> GetAuthStateAsync()
        {
            return await _javaScriptRuntime.InvokeAsync<string>("sessionStorage.getItem", _authenticationStateKey);
        }

        private AuthenticationState BuildAuthenticationState(string email)
        {            
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.Role, "Admin")
            }, "jwt")));
        }
    }
}