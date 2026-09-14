using System;
using eCommerce.Storefront.Model.Orders;
using eCommerce.Storefront.Model.Products;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eCommerce.Storefront.Tests.OrderItemSpecs
{
    [TestClass]
    public class WhenCreatingOrderItemWithZeroQuantity
    {
        private Product _product;

        [TestInitialize]
        public void Given()
        {
            _product = new Product()
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
        }

        [TestMethod]
        public void ThenAnArgumentOutOfRangeExceptionWillBeThrown()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new OrderItem(_product, new Order(), 0));
        }
    }
}