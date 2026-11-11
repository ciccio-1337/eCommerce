using System.Threading.Tasks;
using eCommerce.Storefront.Repository.EntityFrameworkCore;
using eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Implementations;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace eCommerce.Storefront.Tests.Repository
{
    [TestClass]
    public class CustomerRepositoryTests
    {
        private SqliteConnection _connection;
        private ShopDataContext _context;
        private CustomerRepository _repository;

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

            _repository = new CustomerRepository(
                new Mock<IUnitOfWork>().Object,
                _context,
                new UpperInvariantLookupNormalizer());
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        [TestMethod]
        public async Task SaveEmailAsync_WhenEmailChanges_ResetsConfirmationAndSecurityStamp()
        {
            _context.Users.Add(new IdentityUser
            {
                Id = "user-1",
                UserName = "old@test.com",
                Email = "old@test.com",
                NormalizedUserName = "OLD@TEST.COM",
                NormalizedEmail = "OLD@TEST.COM",
                EmailConfirmed = true,
                SecurityStamp = "old-stamp"
            });
            await _context.SaveChangesAsync();

            await _repository.SaveEmailAsync("user-1", "New@Test.com");
            // CustomerService commits through IUnitOfWork; emulate that here.
            await _context.SaveChangesAsync();

            var user = await _context.Users.AsNoTracking().FirstAsync(u => u.Id == "user-1");

            Assert.AreEqual("New@Test.com", user.Email);
            Assert.AreEqual("NEW@TEST.COM", user.NormalizedEmail);
            Assert.AreEqual("NEW@TEST.COM", user.NormalizedUserName);
            Assert.IsFalse(user.EmailConfirmed);
            Assert.AreNotEqual("old-stamp", user.SecurityStamp);
        }

        [TestMethod]
        public async Task SaveEmailAsync_WhenEmailIsUnchanged_DoesNotResetConfirmation()
        {
            _context.Users.Add(new IdentityUser
            {
                Id = "user-1",
                UserName = "old@test.com",
                Email = "old@test.com",
                NormalizedUserName = "OLD@TEST.COM",
                NormalizedEmail = "OLD@TEST.COM",
                EmailConfirmed = true,
                SecurityStamp = "old-stamp"
            });
            await _context.SaveChangesAsync();

            await _repository.SaveEmailAsync("user-1", "old@test.com");

            var user = await _context.Users.AsNoTracking().FirstAsync(u => u.Id == "user-1");

            Assert.IsTrue(user.EmailConfirmed);
            Assert.AreEqual("old-stamp", user.SecurityStamp);
        }
    }
}
