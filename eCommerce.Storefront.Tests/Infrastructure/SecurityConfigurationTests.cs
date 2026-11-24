using System;
using eCommerce.Storefront.UI.Web.MVC.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace eCommerce.Storefront.Tests.Infrastructure
{
    [TestClass]
    public class SecurityConfigurationTests
    {
        [TestMethod]
        public void RequireSecureCookies_IsTrueOutsideDevelopment()
        {
            Assert.IsTrue(SecurityConfiguration.RequireSecureCookies(EnvironmentNamed(Environments.Production)));
            Assert.IsTrue(SecurityConfiguration.RequireSecureCookies(EnvironmentNamed(Environments.Staging)));
        }

        [TestMethod]
        public void RequireSecureCookies_IsFalseInDevelopment()
        {
            Assert.IsFalse(SecurityConfiguration.RequireSecureCookies(EnvironmentNamed(Environments.Development)));
        }

        [TestMethod]
        public void ValidateDataProtectionKeyProtection_ReturnsTrimmedThumbprint()
        {
            var thumbprint = SecurityConfiguration.ValidateDataProtectionKeyProtection(
                EnvironmentNamed(Environments.Production),
                "  ABCDEF0123  ");

            Assert.AreEqual("ABCDEF0123", thumbprint);
        }

        [TestMethod]
        public void ValidateDataProtectionKeyProtection_AllowsMissingThumbprintInDevelopment()
        {
            var thumbprint = SecurityConfiguration.ValidateDataProtectionKeyProtection(
                EnvironmentNamed(Environments.Development),
                null);

            Assert.IsNull(thumbprint);
        }

        [TestMethod]
        public void ValidateDataProtectionKeyProtection_FailsClosedOutsideDevelopment()
        {
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                SecurityConfiguration.ValidateDataProtectionKeyProtection(
                    EnvironmentNamed(Environments.Production),
                    "   "));

            Assert.ThrowsExactly<InvalidOperationException>(() =>
                SecurityConfiguration.ValidateDataProtectionKeyProtection(
                    EnvironmentNamed(Environments.Staging),
                    null));
        }

        [TestMethod]
        public void ValidateAllowedHosts_RejectsWildcardOutsideDevelopment()
        {
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                SecurityConfiguration.ValidateAllowedHosts(
                    EnvironmentNamed(Environments.Production),
                    "*,example.com"));
        }

        [TestMethod]
        public void ValidateAllowedHosts_RejectsEmptyListOutsideDevelopment()
        {
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                SecurityConfiguration.ValidateAllowedHosts(
                    EnvironmentNamed(Environments.Production),
                    "   "));
        }

        [TestMethod]
        public void ValidateAllowedHosts_AcceptsExplicitListOutsideDevelopment()
        {
            SecurityConfiguration.ValidateAllowedHosts(
                EnvironmentNamed(Environments.Production),
                "example.com;www.example.com");
        }

        [TestMethod]
        public void ValidateForwardedHeaders_FailsClosedWhenProxyEnabledWithoutTrustList()
        {
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                SecurityConfiguration.ValidateForwardedHeadersConfiguration(
                    EnvironmentNamed(Environments.Production),
                    reverseProxyEnabled: true,
                    knownProxyCount: 0,
                    knownNetworkCount: 0));
        }

        [TestMethod]
        public void ValidateForwardedHeaders_AllowsDisabledProxyOrTrustedProxy()
        {
            SecurityConfiguration.ValidateForwardedHeadersConfiguration(
                EnvironmentNamed(Environments.Production),
                reverseProxyEnabled: false,
                knownProxyCount: 0,
                knownNetworkCount: 0);

            SecurityConfiguration.ValidateForwardedHeadersConfiguration(
                EnvironmentNamed(Environments.Production),
                reverseProxyEnabled: true,
                knownProxyCount: 1,
                knownNetworkCount: 0);
        }

        [TestMethod]
        public void ValidateForwardedHeaders_ExemptsDevelopment()
        {
            SecurityConfiguration.ValidateForwardedHeadersConfiguration(
                EnvironmentNamed(Environments.Development),
                reverseProxyEnabled: true,
                knownProxyCount: 0,
                knownNetworkCount: 0);
        }

        [TestMethod]
        public void ValidateDatabaseConnectionString_RequiresCertificateValidation()
        {
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                SecurityConfiguration.ValidateDatabaseConnectionString(
                    EnvironmentNamed(Environments.Production),
                    "Host=db;Username=ecommerce;Password=secret;SSL Mode=Prefer"));

            Assert.ThrowsExactly<InvalidOperationException>(() =>
                SecurityConfiguration.ValidateDatabaseConnectionString(
                    EnvironmentNamed(Environments.Production),
                    "Host=db;Username=ecommerce;Password=secret;SSL Mode=Require"));
        }

        [TestMethod]
        public void ValidateDatabaseConnectionString_AcceptsVerifyFull()
        {
            SecurityConfiguration.ValidateDatabaseConnectionString(
                EnvironmentNamed(Environments.Production),
                "Host=db;Username=ecommerce;Password=secret;SSL Mode=VerifyFull");
        }

        [TestMethod]
        public void ValidateDatabaseConnectionString_ExemptsDevelopment()
        {
            SecurityConfiguration.ValidateDatabaseConnectionString(
                EnvironmentNamed(Environments.Development),
                "Host=db;Username=postgres;Password=secret");
        }

        [TestMethod]
        public void ConfigureHsts_SetsLongLivedSubdomainPreloadPolicy()
        {
            var options = new Microsoft.AspNetCore.HttpsPolicy.HstsOptions();

            SecurityConfiguration.ConfigureHsts(options);

            Assert.AreEqual(TimeSpan.FromDays(365), options.MaxAge);
            Assert.IsTrue(options.IncludeSubDomains);
            Assert.IsTrue(options.Preload);
        }

        [TestMethod]
        public void IsSensitiveTokenPath_MatchesTokenEndpoints()
        {
            Assert.IsTrue(SecurityConfiguration.IsSensitiveTokenPath("/AccountRegister/ConfirmEmail"));
            Assert.IsTrue(SecurityConfiguration.IsSensitiveTokenPath("/AccountRegister/SetPassword"));
            Assert.IsFalse(SecurityConfiguration.IsSensitiveTokenPath("/AccountLogOn/LogOn"));
        }

        [TestMethod]
        public void AdminCookie_IsSecurePrefixedAndScopedToApi()
        {
            StringAssert.StartsWith(SecurityConfiguration.AdminCookieName, "__Secure-");
            Assert.AreEqual("/api", SecurityConfiguration.AdminCookiePath);
        }

        private static IHostEnvironment EnvironmentNamed(string environmentName)
        {
            var environment = new Mock<IHostEnvironment>();
            environment.SetupGet(x => x.EnvironmentName).Returns(environmentName);

            return environment.Object;
        }
    }
}
