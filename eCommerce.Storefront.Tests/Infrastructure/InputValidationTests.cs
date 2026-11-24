using System;
using eCommerce.Storefront.Controllers.Models;
using eCommerce.Storefront.Model;
using eCommerce.Storefront.Services.Implementations;
using eCommerce.Storefront.Services.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eCommerce.Storefront.Tests.Infrastructure
{
    [TestClass]
    public class InputValidationTests
    {
        [TestMethod]
        public void CredentialInputValidator_RejectsOverlongEmail()
        {
            Assert.IsFalse(CredentialInputValidator.IsAcceptableEmail(
                new string('a', InputLimits.MaxEmailLength + 1) + "@example.com"));
        }

        [TestMethod]
        public void CredentialInputValidator_RejectsOverlongPassword()
        {
            Assert.IsFalse(CredentialInputValidator.IsAcceptablePassword(
                new string('a', InputLimits.MaxPasswordLength + 1)));
        }

        [TestMethod]
        public void CredentialInputValidator_AcceptsValuesAtTheLimit()
        {
            Assert.IsTrue(CredentialInputValidator.IsAcceptablePassword(
                new string('a', InputLimits.MaxPasswordLength)));
            Assert.IsTrue(CredentialInputValidator.IsAcceptableName(
                new string('a', InputLimits.MaxNameLength)));
        }

        [TestMethod]
        public void CredentialInputValidator_ValidateRegistration_ReturnsMessageForOverlongPassword()
        {
            var message = CredentialInputValidator.ValidateRegistration(
                "user@example.com",
                new string('a', InputLimits.MaxPasswordLength + 1),
                "First",
                "Last");

            Assert.IsNotNull(message);
        }

        [TestMethod]
        public void CredentialInputValidator_ValidateRegistration_ReturnsNullForValidValues()
        {
            var message = CredentialInputValidator.ValidateRegistration(
                "user@example.com",
                "StrongPassword123!",
                "First",
                "Last");

            Assert.IsNull(message);
        }

        [TestMethod]
        public void AccountActionThrottle_BlocksSecondAttemptWithinCooldown()
        {
            IAccountActionThrottle throttle = new AccountActionThrottle();

            Assert.IsTrue(throttle.TryAcquire("user@example.com", TimeSpan.FromMinutes(1)));
            Assert.IsFalse(throttle.TryAcquire("user@example.com", TimeSpan.FromMinutes(1)));
            Assert.IsTrue(throttle.TryAcquire("other@example.com", TimeSpan.FromMinutes(1)));
        }

        [TestMethod]
        public void AccountActionThrottle_AllowsAttemptAfterCooldown()
        {
            IAccountActionThrottle throttle = new AccountActionThrottle();

            Assert.IsTrue(throttle.TryAcquire("user@example.com", TimeSpan.Zero));
            Assert.IsTrue(throttle.TryAcquire("user@example.com", TimeSpan.Zero));
        }

        [TestMethod]
        public void AccountActionThrottle_RejectsBlankKey()
        {
            IAccountActionThrottle throttle = new AccountActionThrottle();

            Assert.IsFalse(throttle.TryAcquire("   ", TimeSpan.FromMinutes(1)));
        }
    }
}
