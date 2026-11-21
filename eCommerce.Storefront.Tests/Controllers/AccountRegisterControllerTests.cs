using System.Threading.Tasks;
using eCommerce.Storefront.Controllers.ActionArguments;
using eCommerce.Storefront.Controllers.Controllers;
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
                normalizer.Object);

            var result = await controller.ResendConfirmation("User@Test.com");

            Assert.IsInstanceOfType(result, typeof(ViewResult));
            // The old code used ToUpperInvariant, which silently diverges from a
            // custom normalizer and makes the resend lookup miss real accounts.
            normalizer.Verify(x => x.NormalizeEmail("User@Test.com"), Times.Once);
        }
    }
}
