using System.Linq;
using eCommerce.Storefront.Model.Basket;
using eCommerce.Storefront.Model.Products;
using eCommerce.Storefront.Model.Shipping;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eCommerce.Storefront.Tests.BasketSpecs
{
    [TestClass]
    public class WhenChangingTheQuantityOfProductThatExisitsInBasketToPositiveAmount
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
        public void ThenTheQuantityOfThatProductShouldUpdateToMatch()
        {
            int newQty = 5;

            _basket.ChangeQtyOfProduct(newQty, _product);

            Assert.AreEqual(newQty, _basket.Items.FirstOrDefault(i => i.Product == _product).Qty);
        }        
    }
}