using System.Linq;
using eCommerce.Storefront.Model.Basket;
using eCommerce.Storefront.Model.Products;
using eCommerce.Storefront.Model.Shipping;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eCommerce.Storefront.Tests.BasketSpecs
{
    [TestClass]
    public class WhenAddingTheSameProductToBasketTwice
    {
        private Basket _basket;
        private Product _product;

        [TestInitialize]
        public void Given()
        {
            _basket = new Basket();

            _basket.SetDeliveryOption(new DeliveryOption());

            _product = new Product()
            {
                Title = new ProductTitle()
                {
                    Name = "Product A",
                    Price = 15m,
                    Brand = new Brand(),
                    Category = new Category(),
                    Color = new ProductColor(),
                },
                Size = new ProductSize()
            };
            
            _basket.Add(_product);
            _basket.Add(_product);
        }

        [TestMethod]
        public void ThenTheBasketTotalShouldBeEqualToTheCostOfTwoPerTheProduct()
        {
            Assert.AreEqual(_product.Price * 2, _basket.BasketTotal);
        }

        [TestMethod]
        public void ThenTheTotalNumberOfItemsInBasketShouldBeEqualToTwo()
        {
            Assert.AreEqual(2, _basket.NumberOfItemsInBasket());
        }

        [TestMethod]
        public void ThenTheBasketItemsTotalShouldBeEqualToTwoPerTheCostOfTheProduct()
        {
            Assert.AreEqual(_product.Price * 2, _basket.ItemsTotal);
        }

        [TestMethod]
        public void ThenTheQuantityForTheProductShouldBeTwo()
        {
            Assert.AreEqual(2, _basket.Items.FirstOrDefault(i => i.Product == _product).Qty);
        }

        [TestMethod]
        public void ThenTheLineTotalForTheProductShouldEqualToTheCostOfTwoPerTheProduct()
        {
            Assert.AreEqual(_product.Price * 2, _basket.Items.FirstOrDefault(i => i.Product == _product).LineTotal());
        }
    }
}