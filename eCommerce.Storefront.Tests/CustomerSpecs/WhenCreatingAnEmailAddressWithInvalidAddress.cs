using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eCommerce.Storefront.Tests.CustomerSpecs
{
    [TestClass]
    public class WhenCreatingAnEmailAddressWithInvalidAddress : WithValidCustomer
    {
        private string _email;

        public override void When()
        {
            _email = "scott@";
            Customer.Email = _email;
        }

        [TestMethod]
        public void ThenTheCustomerShouldHaveOneBrokenRule()
        {
            Assert.HasCount(1, Customer.GetBrokenRules());
        }
    }
}