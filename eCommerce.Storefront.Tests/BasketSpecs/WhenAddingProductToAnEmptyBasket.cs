using System.Linq;
using eCommerce.Storefront.Model.Basket;
using eCommerce.Storefront.Model.Products;
using eCommerce.Storefront.Model.Shipping;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eCommerce.Storefront.Tests.BasketSpecs
{
    [TestClass]
    public class WhenAddingProductToAnEmptyBasket
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
        }

        [TestMethod]
        public void ThenTheBasketTotalShouldBeEqualToTheCostOfTheProduct()
        {
            Assert.AreEqual(_product.Price, _basket.BasketTotal);
        }

        [TestMethod]
        public void ThenTheBasketShouldContainTotalOfOneItem()
        {
            Assert.AreEqual(1, _basket.NumberOfItemsInBasket());
        }

        [TestMethod]
        public void ThenTheBasketShouldContainTotalOfOneOfTheProduct()
        {
            Assert.AreEqual(1, _basket.Items.FirstOrDefault(i => i.Product == _product).Qty);
        }
    }
}