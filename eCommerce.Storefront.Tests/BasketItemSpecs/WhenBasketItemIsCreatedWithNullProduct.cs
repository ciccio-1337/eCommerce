using System.Linq;
using eCommerce.Storefront.Model.Basket;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eCommerce.Storefront.Tests.BasketItemSpecs
{
    [TestClass]
    public class WhenBasketItemIsCreatedWithNullProduct
    {
        private BasketItem _basketItem;

        [TestInitialize]
        public void Given()
        {
            _basketItem = new BasketItem(null, new Basket(), 1);
        }

        [TestMethod]
        public void ThenItShouldHaveBrokenRuleHighlightingTheRequirementForProduct()
        {
            Assert.AreEqual("Product", _basketItem.GetBrokenRules().First(x => true).Property);
        }

        [TestMethod]
        public void ThenItShouldHaveOneBrokenRule()
        {
            Assert.HasCount(1, _basketItem.GetBrokenRules());
        }
    }
}