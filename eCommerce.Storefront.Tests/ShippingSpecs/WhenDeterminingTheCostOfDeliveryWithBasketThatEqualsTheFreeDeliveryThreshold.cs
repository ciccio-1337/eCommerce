using eCommerce.Storefront.Model.Shipping;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eCommerce.Storefront.Tests.ShippingSpecs
{
    [TestClass]
    public class WhenDeterminingTheCostOfDeliveryWithBasketThatEqualsTheFreeDeliveryThreshold
    {
        private DeliveryOption _deliveryOption;
        private decimal _freeDeliveryThreshold;

        [TestInitialize]
        public void Given()
        {
            _freeDeliveryThreshold = 50m;

            _deliveryOption = new DeliveryOption(_freeDeliveryThreshold, 10m, null);
        }

        [TestMethod]
        public void ThenTheCostOfDeliveryShouldBeZero()
        {
            Assert.AreEqual(0, _deliveryOption.GetDeliveryChargeForBasketTotalOf(_freeDeliveryThreshold));
        }
    }
}