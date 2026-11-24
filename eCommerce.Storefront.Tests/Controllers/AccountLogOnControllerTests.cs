using System.Threading.Tasks;
using eCommerce.Storefront.Controllers.ActionArguments;
using eCommerce.Storefront.Controllers.Controllers;
using eCommerce.Storefront.Controllers.Services.Interfaces;
using eCommerce.Storefront.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace eCommerce.Storefront.Tests.Controllers
{
    [TestClass]
    public class AccountLogOnControllerTests
    {
        private Mock<ILocalAuthenticationService> _authenticationServiceMock;
        private AccountLogOnController _controller;

        [TestInitialize]
        public void Setup()
        {
            _authenticationServiceMock = new Mock<ILocalAuthenticationService>();

            _controller = new AccountLogOnController(
                _authenticationServiceMock.Object,
                Mock.Of<ICustomerService>(),
                Mock.Of<ICookieAuthentication>(),
                Mock.Of<IActionArguments>());
        }

        [TestMethod]
        public async Task LogOn_WithOverlongPassword_DoesNotCallIdentity()
        {
            var result = await _controller.LogOn(
                "user@test.com",
                new string('a', eCommerce.Storefront.Model.InputLimits.MaxPasswordLength + 1),
                null);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            _authenticationServiceMock.Verify(
                x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [TestMethod]
        public async Task LogOn_WithOverlongEmail_DoesNotCallIdentity()
        {
            var email = new string('a', eCommerce.Storefront.Model.InputLimits.MaxEmailLength + 1) + "@test.com";

            var result = await _controller.LogOn(email, "StrongPassword123!", null);

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            _authenticationServiceMock.Verify(
                x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }
    }
}
