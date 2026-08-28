using System;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using eCommerce.Backoffice.Client.Services.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace eCommerce.Backoffice.Client.Services.Implementations
{
    public class CustomAuthStateProvider(IJSRuntime javaScriptRuntime) : AuthenticationStateProvider, ILoginService
    {
        private readonly IJSRuntime _javaScriptRuntime = javaScriptRuntime;
        private static readonly string _authenticationStateKey = "AuthenticationState";
        private static AuthenticationState Anonymous => new(new ClaimsPrincipal(new ClaimsIdentity()));

        public async override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var authState = await GetAuthStateAsync();

            if (string.IsNullOrWhiteSpace(authState))
            {
                await _javaScriptRuntime.InvokeVoidAsync("sessionStorage.removeItem", _authenticationStateKey);

                return Anonymous;
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

            NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
        }

        public async Task<string> GetAuthStateAsync()
        {
            return await _javaScriptRuntime.InvokeAsync<string>("sessionStorage.getItem", _authenticationStateKey);
        }

        private static AuthenticationState BuildAuthenticationState(string email)
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity([
                new(ClaimTypes.Name, email),
                new(ClaimTypes.Role, "Admin")
            ], "jwt")));
        }
    }
}