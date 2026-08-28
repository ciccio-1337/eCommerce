using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eCommerce.Storefront.Tests.CustomerSpecs
{
    [TestClass]
    public class WhenUpdatingCustomersEmailAddressWithNullValue : WithValidCustomer
    {
        public override void When()
        {
            Customer.Email = null;
        }

        [TestMethod]
        public void ThenTheCustomerShouldHaveOneBrokenRule()
        {
            Assert.HasCount(1, Customer.GetBrokenRules());
        }
    }
}