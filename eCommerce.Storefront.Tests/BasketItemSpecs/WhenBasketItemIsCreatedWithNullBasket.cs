using System.Linq;
using eCommerce.Storefront.Model.Basket;
using eCommerce.Storefront.Model.Products;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eCommerce.Storefront.Tests.BasketItemSpecs
{
    [TestClass]
    public class WhenBasketItemIsCreatedWithNullBasket
    {
        private BasketItem _basketItem;

        [TestInitialize]
        public void Given()
        {
            _basketItem = new BasketItem(new Product(), null, 1);
        }

        [TestMethod]
        public void ThenItShouldHaveOneBrokenRule()
        {
            Assert.HasCount(1, _basketItem.GetBrokenRules());
        }

        [TestMethod]
        public void ThenItShouldHaveBrokenRuleHighlightingTheRequirementForBasket()
        {
            Assert.AreEqual("Basket", _basketItem.GetBrokenRules().First(x => true).Property);
        }
    }
}