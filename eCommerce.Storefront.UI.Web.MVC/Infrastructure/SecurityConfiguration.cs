using System;
using System.Linq;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace eCommerce.Storefront.UI.Web.MVC.Infrastructure
{
    /// <summary>
    /// Small, testable security decisions shared by startup code. Keeping these
    /// out of <see cref="Program"/> makes the posture explicit and unit-testable.
    /// </summary>
    public static class SecurityConfiguration
    {
        // The backoffice cookie is scoped to the API surface so it is never sent
        // with storefront requests. The __Secure- prefix requires Secure, which the
        // cookie policy already enforces outside development.
        public const string AdminCookieName = "__Secure-ecommerce-admin";
        public const string AdminCookiePath = "/api";

        /// <summary>
        /// Authentication, backoffice and antiforgery cookies must always be
        /// marked Secure outside local development. Staging and QA frequently run
        /// behind a TLS-terminating proxy with <c>ASPNETCORE_ENVIRONMENT=Staging</c>,
        /// so keying the decision off <c>IsProduction()</c> would emit non-Secure
        /// cookies there and allow them to travel over plain HTTP.
        /// </summary>
        public static bool RequireSecureCookies(IHostEnvironment environment)
        {
            ArgumentNullException.ThrowIfNull(environment);

            return !environment.IsDevelopment();
        }

        /// <summary>
        /// Returns the trimmed data-protection certificate thumbprint, or
        /// <c>null</c> in development. Outside development a thumbprint is
        /// mandatory: persisting unprotected key files lets anyone who can read
        /// them forge authentication and antiforgery cookies for any account.
        /// </summary>
        public static string ValidateDataProtectionKeyProtection(
            IHostEnvironment environment,
            string certificateThumbprint)
        {
            ArgumentNullException.ThrowIfNull(environment);

            var thumbprint = certificateThumbprint?.Trim();

            if (!string.IsNullOrEmpty(thumbprint))
            {
                return thumbprint;
            }

            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "DataProtection:CertificateThumbprint must be configured in non-development "
                    + "environments so persisted data-protection keys are encrypted at rest.");
            }

            return null;
        }

        /// <summary>
        /// Rejects a host allow-list that contains the catch-all wildcard. A value
        /// such as <c>"*,example.com"</c> used to pass the old equality check.
        /// </summary>
        public static void ValidateAllowedHosts(IHostEnvironment environment, string allowedHosts)
        {
            ArgumentNullException.ThrowIfNull(environment);

            if (environment.IsDevelopment())
            {
                return;
            }

            var hosts = (allowedHosts ?? string.Empty)
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (hosts.Length == 0 || hosts.Any(host => host.Contains('*', StringComparison.Ordinal)))
            {
                throw new InvalidOperationException(
                    "AllowedHosts must be an explicit host list in non-development environments; "
                    + "the wildcard '*' is not allowed.");
            }
        }

        /// <summary>
        /// When the application is told it runs behind a proxy, at least one
        /// trusted proxy or network must be configured. Otherwise every request
        /// appears to originate from the proxy, collapsing the per-client rate
        /// limits into one shared bucket.
        /// </summary>
        public static void ValidateForwardedHeadersConfiguration(
            IHostEnvironment environment,
            bool reverseProxyEnabled,
            int knownProxyCount,
            int knownNetworkCount)
        {
            ArgumentNullException.ThrowIfNull(environment);

            if (environment.IsDevelopment() || !reverseProxyEnabled)
            {
                return;
            }

            if (knownProxyCount <= 0 && knownNetworkCount <= 0)
            {
                throw new InvalidOperationException(
                    "ReverseProxy:Enabled is true but no ReverseProxy:KnownProxies or "
                    + "ReverseProxy:KnownNetworks are configured. Rate limiting would key on the "
                    + "proxy address; configure the trusted proxy list.");
            }
        }

        /// <summary>
        /// Outside development the PostgreSQL connection must verify the server
        /// certificate. <c>Require</c> encrypts but does not authenticate the server;
        /// <c>Prefer</c>/<c>Allow</c>/<c>Disable</c> can be downgraded to plaintext.
        /// </summary>
        public static void ValidateDatabaseConnectionString(
            IHostEnvironment environment,
            string connectionString)
        {
            ArgumentNullException.ThrowIfNull(environment);

            if (environment.IsDevelopment())
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:DefaultConnection must be configured in non-development environments.");
            }

            NpgsqlConnectionStringBuilder builder;

            try
            {
                builder = new NpgsqlConnectionStringBuilder(connectionString);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:DefaultConnection is not a valid PostgreSQL connection string.",
                    ex);
            }

            if (builder.SslMode is not (SslMode.VerifyCA or SslMode.VerifyFull))
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:DefaultConnection must use SSL Mode=VerifyCA or VerifyFull "
                    + "outside development so the database server certificate is validated.");
            }
        }

        /// <summary>
        /// HSTS is only honoured over HTTPS. A one-year lifetime with subdomains
        /// and preload closes the SSL-stripping window for the whole site.
        /// </summary>
        public static void ConfigureHsts(HstsOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            options.MaxAge = TimeSpan.FromDays(365);
            options.IncludeSubDomains = true;
            options.Preload = true;
        }

        /// <summary>
        /// Paths whose query string carries a single-use security token. They must
        /// not leak the token to third parties through the Referer header.
        /// </summary>
        public static bool IsSensitiveTokenPath(PathString path)
        {
            return path.StartsWithSegments("/AccountRegister/ConfirmEmail", StringComparison.OrdinalIgnoreCase)
                || path.StartsWithSegments("/AccountRegister/SetPassword", StringComparison.OrdinalIgnoreCase)
                || path.StartsWithSegments("/admin/account/changepassword", StringComparison.OrdinalIgnoreCase)
                || path.StartsWithSegments("/admin/account/emailconfirmation", StringComparison.OrdinalIgnoreCase);
        }
    }
}
