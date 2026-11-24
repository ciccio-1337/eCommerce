using System.Threading.Tasks;
using eCommerce.Storefront.Controllers.ActionArguments;
using eCommerce.Storefront.Controllers.Controllers;
using eCommerce.Storefront.Controllers.Models;
using eCommerce.Storefront.Controllers.Services.Interfaces;
using eCommerce.Storefront.Repository.EntityFrameworkCore;
using eCommerce.Storefront.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace eCommerce.Storefront.Tests.Controllers
{
    [TestClass]
    public class AccountRegisterControllerTests
    {
        private SqliteConnection _connection;
        private ShopDataContext _context;

        [TestInitialize]
        public void Setup()
        {
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<ShopDataContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new ShopDataContext(options);
            _context.Database.EnsureCreated();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        [TestMethod]
        public async Task ResendConfirmation_UsesTheConfiguredLookupNormalizer()
        {
            var normalizer = new Mock<ILookupNormalizer>();
            normalizer
                .Setup(x => x.NormalizeEmail("User@Test.com"))
                .Returns("user@test.com");

            var controller = new AccountRegisterController(
                Mock.Of<ILocalAuthenticationService>(),
                Mock.Of<ICustomerService>(),
                Mock.Of<ICookieAuthentication>(),
                Mock.Of<IActionArguments>(),
                Mock.Of<IEmailService>(),
                Mock.Of<IConfiguration>(),
                Mock.Of<IWebHostEnvironment>(),
                Mock.Of<ILogger<AccountRegisterController>>(),
                _context,
                normalizer.Object,
                Mock.Of<IAccountActionThrottle>());

            var result = await controller.ResendConfirmation("User@Test.com");

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            // The old code used ToUpperInvariant, which silently diverges from a
            // custom normalizer and makes the resend lookup miss real accounts.
            normalizer.Verify(x => x.NormalizeEmail("User@Test.com"), Times.Once);
        }

        [TestMethod]
        public async Task Register_WithOverlongPassword_ReturnsFormWithoutCallingIdentity()
        {
            var authentication = new Mock<ILocalAuthenticationService>();
            var controller = CreateController(authentication);

            var result = await controller.Register(
                new string('a', 129),
                "user@test.com",
                "First",
                "Last",
                null);

            var view = result as ViewResult;

            Assert.IsNotNull(view);
            Assert.AreEqual("Register", view.ViewName);
            authentication.Verify(
                x => x.RegisterUserAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<System.Collections.Generic.IEnumerable<string>>()),
                Times.Never);
        }

        [TestMethod]
        public async Task Register_WhenEmailAlreadyExists_ReturnsNeutralPendingView()
        {
            var authentication = new Mock<ILocalAuthenticationService>();
            authentication
                .Setup(x => x.RegisterUserAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<System.Collections.Generic.IEnumerable<string>>()))
                .ThrowsAsync(new AccountAlreadyExistsException());

            var controller = CreateController(authentication);

            var result = await controller.Register(
                "StrongPassword123!",
                "existing@test.com",
                "First",
                "Last",
                null);

            var view = result as ViewResult;

            Assert.IsNotNull(view);
            Assert.AreEqual("RegistrationPending", view.ViewName);
        }

        [TestMethod]
        public async Task ConfirmEmail_RotatesTheRegistrationPasswordAndShowsSetPassword()
        {
            _context.Users.Add(new IdentityUser
            {
                Id = "user-1",
                UserName = "user@test.com",
                Email = "user@test.com"
            });
            await _context.SaveChangesAsync();

            var authentication = new Mock<ILocalAuthenticationService>();
            authentication.Setup(x => x.ConfirmEmailAsync("user-1", "confirm-code")).ReturnsAsync(true);
            authentication
                .SetupSequence(x => x.GeneratePasswordResetTokenAsync("user-1"))
                .ReturnsAsync("rotation-code")
                .ReturnsAsync("set-password-code");

            string capturedPassword = null;
            authentication
                .Setup(x => x.ResetPasswordAsync("user-1", "rotation-code", It.IsAny<string>()))
                .Callback<string, string, string>((_, _, password) => capturedPassword = password)
                .ReturnsAsync(true);

            var controller = CreateController(authentication);

            var result = await controller.ConfirmEmail("user-1", "confirm-code");

            var view = result as ViewResult;

            Assert.IsNotNull(view);
            Assert.AreEqual("SetPassword", view.ViewName);
            Assert.IsNotNull(capturedPassword);
            // The registration password must be replaced with a value that satisfies
            // Identity's policy, otherwise the attacker-chosen password survives.
            StringAssert.StartsWith(capturedPassword, "A1a!");
            authentication.Verify(x => x.GeneratePasswordResetTokenAsync("user-1"), Times.Exactly(2));
        }

        [TestMethod]
        public async Task ConfirmEmail_WhenPasswordRotationFails_DoesNotShowSetPassword()
        {
            _context.Users.Add(new IdentityUser
            {
                Id = "user-1",
                UserName = "user@test.com",
                Email = "user@test.com"
            });
            await _context.SaveChangesAsync();

            var authentication = new Mock<ILocalAuthenticationService>();
            authentication.Setup(x => x.ConfirmEmailAsync("user-1", "confirm-code")).ReturnsAsync(true);
            authentication
                .Setup(x => x.GeneratePasswordResetTokenAsync("user-1"))
                .ReturnsAsync("rotation-code");
            authentication
                .Setup(x => x.ResetPasswordAsync("user-1", "rotation-code", It.IsAny<string>()))
                .ReturnsAsync(false);

            var controller = CreateController(authentication);

            var result = await controller.ConfirmEmail("user-1", "confirm-code");

            var view = result as ViewResult;

            Assert.IsNotNull(view);
            // A null ViewName resolves to the action name (ConfirmEmail) at runtime.
            Assert.IsNull(view.ViewName);
            Assert.IsFalse((bool)view.ViewData["Confirmed"]);
        }

        private AccountRegisterController CreateController(Mock<ILocalAuthenticationService> authentication)
        {
            return new AccountRegisterController(
                authentication.Object,
                Mock.Of<ICustomerService>(),
                Mock.Of<ICookieAuthentication>(),
                Mock.Of<IActionArguments>(),
                Mock.Of<IEmailService>(),
                Mock.Of<IConfiguration>(),
                Mock.Of<IWebHostEnvironment>(),
                Mock.Of<ILogger<AccountRegisterController>>(),
                _context,
                Mock.Of<ILookupNormalizer>(),
                Mock.Of<IAccountActionThrottle>());
        }
    }
}
