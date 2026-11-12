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

        private static IHostEnvironment EnvironmentNamed(string environmentName)
        {
            var environment = new Mock<IHostEnvironment>();
            environment.SetupGet(x => x.EnvironmentName).Returns(environmentName);

            return environment.Object;
        }
    }
}
