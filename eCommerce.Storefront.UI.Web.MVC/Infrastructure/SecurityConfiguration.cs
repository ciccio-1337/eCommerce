using System;
using Microsoft.Extensions.Hosting;

namespace eCommerce.Storefront.UI.Web.MVC.Infrastructure
{
    /// <summary>
    /// Small, testable security decisions shared by startup code. Keeping these
    /// out of <see cref="Program"/> makes the posture explicit and unit-testable.
    /// </summary>
    public static class SecurityConfiguration
    {
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
    }
}
