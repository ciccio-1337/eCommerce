using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace eCommerce.Backoffice.Client.Services.Implementations
{
    /// <summary>
    /// Sends the antiforgery request token on every state-changing request made
    /// through the shared <see cref="HttpClient"/>.
    /// </summary>
    public sealed class AntiforgeryTokenHandler(AntiforgeryTokenProvider tokenProvider) : DelegatingHandler(new HttpClientHandler())
    {
        private const string HeaderName = "RequestVerificationToken";

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Method != HttpMethod.Get
                && request.Method != HttpMethod.Head
                && request.Method != HttpMethod.Options)
            {
                var token = await tokenProvider.GetTokenAsync();

                request.Headers.Remove(HeaderName);
                request.Headers.TryAddWithoutValidation(HeaderName, token);
            }

            var response = await base.SendAsync(request, cancellationToken);

            // A rejected token is usually stale (for example after a sign-in
            // changed the identity it is bound to). Drop it so the next request
            // fetches a fresh one instead of failing forever.
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                tokenProvider.Invalidate();
            }

            return response;
        }
    }
}
