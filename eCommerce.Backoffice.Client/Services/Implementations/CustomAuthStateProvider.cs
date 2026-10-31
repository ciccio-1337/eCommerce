using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;
using eCommerce.Backoffice.Client.Services.Interfaces;
using eCommerce.Backoffice.Shared.Model.Accounts;
using Microsoft.AspNetCore.Components.Authorization;

namespace eCommerce.Backoffice.Client.Services.Implementations
{
    /// <summary>
    /// Builds the client authentication state from the server. The browser never
    /// decides who the user is or which roles they hold: a successful
    /// <c>GET /api/accounts/me</c> response is the only source of truth.
    /// </summary>
    public class CustomAuthStateProvider(
        HttpClient httpClient,
        AntiforgeryTokenProvider antiforgeryTokenProvider) : AuthenticationStateProvider, ILoginService
    {
        private static AuthenticationState Anonymous => new(new ClaimsPrincipal(new ClaimsIdentity()));

        public async override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var currentUser = await httpClient.GetFromJsonAsync<CurrentUserResponse>("api/accounts/me");

                if (currentUser is null || string.IsNullOrWhiteSpace(currentUser.Email))
                {
                    return Anonymous;
                }

                return BuildAuthenticationState(currentUser);
            }
            catch
            {
                // Unauthenticated, server unreachable, or a non-success response.
                return Anonymous;
            }
        }

        public async Task LoginAsync()
        {
            antiforgeryTokenProvider.Invalidate();

            var authenticationState = await GetAuthenticationStateAsync();

            NotifyAuthenticationStateChanged(Task.FromResult(authenticationState));
        }

        public Task LogoutAsync()
        {
            antiforgeryTokenProvider.Invalidate();

            NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));

            return Task.CompletedTask;
        }

        private static AuthenticationState BuildAuthenticationState(CurrentUserResponse currentUser)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, currentUser.Email)
            };

            foreach (var role in currentUser.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims, "server")));
        }
    }
}
