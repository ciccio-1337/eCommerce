using System;
using Mapster;
using eCommerce.Storefront.Model.Customers;
using eCommerce.Storefront.Model.Orders;
using eCommerce.Storefront.Model.Products;
using eCommerce.Storefront.Model.Shipping;
using eCommerce.Storefront.Services;
using eCommerce.Storefront.Services.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eCommerce.Storefront.Tests.Infrastructure
{
    [TestClass]
    public class MappingTests
    {
        private TypeAdapterConfig _config;

        [TestInitialize]
        public void Setup()
        {
            _config = new TypeAdapterConfig();

            new MapsterBootStrapper().Register(_config);
        }

        [TestMethod]
        public void OrderToOrderView_WhenOrderIsPaid_MapsOrderHasBeenPaidFor()
        {
            var order = CreateValidOrder();
            order.SetPayment(new Payment(DateTime.UtcNow, "TXN-1", "PayPal", order.Total()));

            var view = order.Adapt<OrderView>(_config);

            Assert.IsTrue(view.OrderHasBeenPaidFor);
            Assert.AreEqual("TXN-1", view.PaymentTransactionId);
            Assert.IsTrue(view.PaymentDatePaid > DateTime.MinValue);
        }

        [TestMethod]
        public void OrderToOrderView_WhenOrderIsUnpaid_MapsOrderHasBeenPaidForAsFalse()
        {
            var order = CreateValidOrder();

            var view = order.Adapt<OrderView>(_config);

            Assert.IsFalse(view.OrderHasBeenPaidFor);
        }

        [TestMethod]
        public void CategoryToCategoryView_PreservesLongIdentifier()
        {
            var id = (long)int.MaxValue + 42;
            var category = new Category { Id = id, Name = "Long id category" };

            var view = category.Adapt<CategoryView>(_config);

            Assert.AreEqual(id, view.Id);
            Assert.AreEqual("Long id category", view.Name);
        }

        private static Order CreateValidOrder()
        {
            var product = new Product
            {
                Title = new ProductTitle
                {
                    Name = "Product A",
                    Price = 15m,
                    Brand = new Brand(),
                    Category = new Category(),
                    Color = new ProductColor()
                },
                Size = new ProductSize()
            };
            var order = new Order();

            order.AddItem(product, 1);
            order.Customer = new Customer
            {
                FirstName = "John",
                SecondName = "Doe",
                Email = "john@test.com",
                UserId = "user-1"
            };
            order.DeliveryAddress = new DeliveryAddress
            {
                Name = "John Doe",
                AddressLine = "1 Main Street",
                City = "London",
                Country = "UK",
                ZipCode = "N1 1AA"
            };
            order.ShippingService = new ShippingService("STD", "Standard", new Courier());
            order.ShippingCharge = 5m;

            return order;
        }
    }
}
