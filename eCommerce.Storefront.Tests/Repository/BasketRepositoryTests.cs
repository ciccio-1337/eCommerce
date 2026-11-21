using System.Linq;
using System.Threading.Tasks;
using eCommerce.Storefront.Model.Basket;
using eCommerce.Storefront.Model.Customers;
using eCommerce.Storefront.Model.Products;
using eCommerce.Storefront.Model.Shipping;
using eCommerce.Storefront.Repository.EntityFrameworkCore;
using eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Implementations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eCommerce.Storefront.Tests.Repository
{
    [TestClass]
    public class BasketRepositoryTests
    {
        private SqliteConnection _connection;
        private ShopDataContext _context;
        private BasketRepository _repository;

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

            _repository = new BasketRepository(new UnitOfWork(_context), _context);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        [TestMethod]
        public async Task RemovingTheLastItem_IsPersistedWhenTheBasketIsSaved()
        {
            var customer = new Customer
            {
                UserId = "user-1",
                FirstName = "John",
                SecondName = "Doe"
            };
            _context.Add(customer);

            var product = await _context.Set<Product>().FirstAsync(p => p.Id == 1);
            var basket = new Basket();
            basket.SetCustomer(customer);
            basket.SetDeliveryOption(new DeliveryOption());
            basket.Add(product);
            customer.AddBasket(basket);
            _context.Add(basket);

            await _context.SaveChangesAsync();

            var basketId = basket.Id;
            _context.ChangeTracker.Clear();

            var loaded = await _repository.FindByAsync(basketId);

            Assert.IsNotNull(loaded);
            Assert.AreEqual(1, loaded.Items.Count);

            loaded.Remove(loaded.Items[0].Product);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            var reloaded = await _repository.FindByAsync(basketId);

            // The bug was an early return before SaveChanges, so the BasketItem row
            // survived and the "removed" line reappeared on the next page load.
            Assert.AreEqual(0, reloaded.Items.Count);
        }
    }
}
