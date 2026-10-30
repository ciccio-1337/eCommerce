using System;
using eCommerce.Storefront.Model.Orders;
using eCommerce.Storefront.Model.Products;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eCommerce.Storefront.Tests.Infrastructure
{
    [TestClass]
    public class ModelValidationTests
    {
        [TestMethod]
        public void Product_WithNullTitle_NameReturnsNull()
        {
            var product = new Product { Title = null };
            Assert.IsNull(product.Name);
        }

        [TestMethod]
        public void Product_WithNullTitle_PriceReturnsZero()
        {
            var product = new Product { Title = null };
            Assert.AreEqual(0m, product.Price);
        }

        [TestMethod]
        public void Product_WithNullTitle_BrandReturnsNull()
        {
            var product = new Product { Title = null };
            Assert.IsNull(product.Brand);
        }

        [TestMethod]
        public void Payment_ValidatesPositiveAmount()
        {
            try
            {
                new Payment(DateTime.UtcNow, "TXN123", "PayPal", -10m);
                Assert.Fail("Expected exception");
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(eCommerce.Storefront.Model.ValueObjectIsInvalidException));
            }
        }

        [TestMethod]
        public void Payment_ValidatesTransactionId()
        {
            try
            {
                new Payment(DateTime.UtcNow, "", "PayPal", 10m);
                Assert.Fail("Expected exception");
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(eCommerce.Storefront.Model.ValueObjectIsInvalidException));
            }
        }

        [TestMethod]
        public void Order_SetPayment_ThrowsIfAlreadyPaid()
        {
            var order = CreateValidOrder();
            var payment1 = new Payment(DateTime.UtcNow, "TXN1", "PayPal", order.Total());
            var payment2 = new Payment(DateTime.UtcNow, "TXN2", "PayPal", order.Total());
            order.SetPayment(payment1);
            Assert.ThrowsExactly<OrderAlreadyPaidForException>(() => order.SetPayment(payment2));
        }

        [TestMethod]
        public void Order_SetPayment_ThrowsIfAmountMismatch()
        {
            var order = CreateValidOrder();
            var payment = new Payment(DateTime.UtcNow, "TXN1", "PayPal", order.Total() - 1);
            Assert.ThrowsExactly<PaymentAmountDoesNotEqualOrderTotalException>(() => order.SetPayment(payment));
        }

        private Order CreateValidOrder()
        {
            var product = new Product()
            {
                Title = new ProductTitle()
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
            order.Customer = new eCommerce.Storefront.Model.Customers.Customer { FirstName = "John", SecondName = "Doe", Email = "john@test.com", UserId = "123" };
            order.DeliveryAddress = new eCommerce.Storefront.Model.Customers.DeliveryAddress();
            order.ShippingService = new eCommerce.Storefront.Model.Shipping.ShippingService("STD", "Standard", new eCommerce.Storefront.Model.Shipping.Courier());
            order.ShippingCharge = 5m;
            return order;
        }
    }
}
