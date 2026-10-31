using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using eCommerce.Backoffice.Shared.Model.Security;
using Microsoft.AspNetCore.Components;

namespace eCommerce.Backoffice.Client.Services.Implementations
{
    /// <summary>
    /// Fetches and caches the antiforgery request token for the current session.
    /// The token is tied to the antiforgery cookie and, once the user signs in,
    /// to the authenticated identity; callers must invalidate it after a
    /// login/logout so the next write request gets a token for the new identity.
    /// </summary>
    public sealed class AntiforgeryTokenProvider(NavigationManager navigationManager)
    {
        private readonly HttpClient _httpClient = new() { BaseAddress = new Uri(navigationManager.BaseUri) };
        private readonly SemaphoreSlim _lock = new(1, 1);
        private string _token;

        public async Task<string> GetTokenAsync()
        {
            if (!string.IsNullOrWhiteSpace(_token))
            {
                return _token;
            }

            await _lock.WaitAsync();

            try
            {
                if (!string.IsNullOrWhiteSpace(_token))
                {
                    return _token;
                }

                var response = await _httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>("api/antiforgery/token");

                if (string.IsNullOrWhiteSpace(response?.Token))
                {
                    throw new InvalidOperationException("The server did not return an antiforgery token.");
                }

                _token = response.Token;

                return _token;
            }
            finally
            {
                _lock.Release();
            }
        }

        public void Invalidate()
        {
            _token = null;
        }
    }
}
