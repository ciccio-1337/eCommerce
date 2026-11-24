using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace eCommerce.Storefront.Controllers
{
    public static class UrlHelper
    {
        public static string Resolve(this HttpContext httpContext, string resource)
        {
            // Prefer the configured public base address so links sent to third
            // parties (for example PayPal return/notify URLs) cannot be poisoned
            // through the request Host header. Outside Development a missing value
            // is a configuration error, not a reason to trust the Host header.
            var configuration = httpContext.RequestServices.GetService<IConfiguration>();
            var configuredBaseUrl = configuration?["App:PublicBaseUrl"];
            var baseUrl = configuredBaseUrl;

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                var environment = httpContext.RequestServices.GetService<IHostEnvironment>();

                if (environment is null || !environment.IsDevelopment())
                {
                    throw new InvalidOperationException(
                        "App:PublicBaseUrl must be configured in non-development environments.");
                }

                baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host.Value}";
            }

            return $"{baseUrl.TrimEnd('/')}/{resource?.TrimStart('/')}";
        }
    }
}
