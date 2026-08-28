using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eCommerce.Storefront.Tests.CustomerSpecs
{
    [TestClass]
    public class WhenUpdatingCustomersEmailAddress : WithValidCustomer
    {
        private string _email;

        public override void When()
        {
            _email = new string("Scott@elbandit.co.uk");

            Customer.Email = _email;
        }

        [TestMethod]
        public void ThenTheCustomerEmailPropertyWillBeSet()
        {
            Assert.AreEqual(_email, Customer.Email);
        }
    }
}