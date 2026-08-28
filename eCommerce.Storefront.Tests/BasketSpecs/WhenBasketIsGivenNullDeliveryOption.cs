using System.Linq;
using eCommerce.Storefront.Model.Basket;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eCommerce.Storefront.Tests.BasketSpecs
{
    [TestClass]
    public class WhenBasketIsGivenNullDeliveryOption
    {
        private Basket _basket;

        [TestInitialize]
        public void Given()
        {
            _basket = new Basket();

            _basket.SetDeliveryOption(null);
        }

        [TestMethod]
        public void ThenTheBasketShouldHaveTwoBrokenRules()
        {
            Assert.HasCount(2, _basket.GetBrokenRules());
        }

        [TestMethod]
        public void ThenTheBasketShouldHaveBrokenRuleHighlightingTheInvalidDeliveryOption()
        {
            Assert.AreEqual("DeliveryOption", _basket.GetBrokenRules().First(x => true).Property);
        }
    }
}
