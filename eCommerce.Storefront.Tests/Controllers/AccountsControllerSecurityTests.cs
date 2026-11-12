using System.Reflection;
using System.Threading.Tasks;
using eCommerce.Backoffice.Server.Controllers;
using eCommerce.Backoffice.Shared.Model.Accounts;
using eCommerce.Backoffice.Shared.Services.Interfaces;
using eCommerce.Storefront.Model.Customers;
using eCommerce.Storefront.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace eCommerce.Storefront.Tests.Controllers
{
    [TestClass]
    public class AccountsControllerSecurityTests
    {
        private Mock<UserManager<IdentityUser>> _userManagerMock;
        private Mock<SignInManager<IdentityUser>> _signInManagerMock;
        private Mock<IEmailService> _emailServiceMock;
        private AccountsController _controller;

        [TestInitialize]
        public void Setup()
        {
            _userManagerMock = new Mock<UserManager<IdentityUser>>(
                Mock.Of<IUserStore<IdentityUser>>(),
                null,
                new PasswordHasher<IdentityUser>(),
                null,
                null,
                null,
                null,
                null,
                null);

            _signInManagerMock = new Mock<SignInManager<IdentityUser>>(
                _userManagerMock.Object,
                Mock.Of<IHttpContextAccessor>(),
                Mock.Of<IUserClaimsPrincipalFactory<IdentityUser>>(),
                null,
                null,
                null,
                null);

            _emailServiceMock = new Mock<IEmailService>();

            _controller = new AccountsController(
                _emailServiceMock.Object,
                _signInManagerMock.Object,
                Mock.Of<IConfiguration>(),
                Mock.Of<IEntityService<Customer, long>>(),
                null,
                null,
                Mock.Of<ILogger<AccountsController>>());
        }

        [TestMethod]
        public void PublicEndpoints_AreExplicitlyAnonymous()
        {
            // The application uses a global fallback authorization policy. Any
            // pre-authentication endpoint that is not marked [AllowAnonymous]
            // would answer 401 before its action runs.
            AssertIsAnonymous(nameof(AccountsController.Login));
            AssertIsAnonymous(nameof(AccountsController.ForgotPassword));
            AssertIsAnonymous(nameof(AccountsController.EmailConfirmation));
            AssertIsAnonymous(nameof(AccountsController.ChangePassword));
        }

        [TestMethod]
        public async Task Login_WhenTwoFactorEnabled_ReturnsGenericErrorAndNoSession()
        {
            var user = new IdentityUser { Id = "admin-1", Email = "admin@test.com" };

            _userManagerMock.Setup(x => x.FindByEmailAsync("admin@test.com")).ReturnsAsync(user);
            _signInManagerMock
                .Setup(x => x.CheckPasswordSignInAsync(user, "password", true))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
            _userManagerMock.Setup(x => x.GetTwoFactorEnabledAsync(user)).ReturnsAsync(true);

            var result = await _controller.Login(new LoginRequest
            {
                Email = "admin@test.com",
                Password = "password"
            });

            var ok = result.Result as OkObjectResult;

            Assert.IsNotNull(ok);

            var response = ok.Value as LoginResponse;

            Assert.IsNotNull(response);
            Assert.IsFalse(response.IsSuccess);
            CollectionAssert.Contains(new System.Collections.Generic.List<string>(response.Errors), "Username and password are invalid.");
            _signInManagerMock.Verify(
                x => x.CreateUserPrincipalAsync(It.IsAny<IdentityUser>()),
                Times.Never);
        }

        private static void AssertIsAnonymous(string actionName)
        {
            var method = typeof(AccountsController).GetMethod(actionName);

            Assert.IsNotNull(method, $"Action {actionName} was not found.");
            Assert.IsNotNull(
                method.GetCustomAttribute<AllowAnonymousAttribute>(),
                $"Action {actionName} must be marked [AllowAnonymous].");
        }

        [TestMethod]
        public async Task ForgotPassword_ForNonAdminCustomer_DoesNotSendResetMail()
        {
            var customer = new IdentityUser { Id = "customer-1", Email = "customer@test.com" };

            _userManagerMock.Setup(x => x.FindByEmailAsync("customer@test.com")).ReturnsAsync(customer);
            _userManagerMock.Setup(x => x.IsEmailConfirmedAsync(customer)).ReturnsAsync(true);
            _userManagerMock.Setup(x => x.IsInRoleAsync(customer, "Admin")).ReturnsAsync(false);

            var result = await _controller.ForgotPassword(new ForgotPasswordRequest
            {
                Email = "customer@test.com"
            });

            var ok = result.Result as OkObjectResult;

            Assert.IsNotNull(ok);
            Assert.IsTrue(((ForgotPasswordResponse)ok.Value).IsSuccess);
            _emailServiceMock.Verify(
                x => x.SendMailAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>()),
                Times.Never);
            _userManagerMock.Verify(
                x => x.GeneratePasswordResetTokenAsync(It.IsAny<IdentityUser>()),
                Times.Never);
        }
    }
}
