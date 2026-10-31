using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace eCommerce.Storefront.Controllers
{
    public static class UrlHelper
    {
        public static string Resolve(this HttpContext httpContext, string resource)
        {
            // Prefer the configured public base address so links sent to third
            // parties (for example PayPal return/notify URLs) cannot be poisoned
            // through the request Host header.
            var configuration = httpContext.RequestServices.GetService<IConfiguration>();
            var configuredBaseUrl = configuration?["App:PublicBaseUrl"];

            var baseUrl = string.IsNullOrWhiteSpace(configuredBaseUrl)
                ? $"{httpContext.Request.Scheme}://{httpContext.Request.Host.Value}"
                : configuredBaseUrl;

            return $"{baseUrl.TrimEnd('/')}/{resource?.TrimStart('/')}";
        }
    }
}
