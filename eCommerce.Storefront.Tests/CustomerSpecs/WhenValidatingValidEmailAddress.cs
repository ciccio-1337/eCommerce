using eCommerce.Storefront.Model.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eCommerce.Storefront.Tests.CustomerSpecs
{
    [TestClass]
    public class WhenValidatingValidEmailAddress
    {
        private string _validEmailAddress;

        [TestInitialize]
        public void Given()
        {
            _validEmailAddress = "scott@elbandit.co.uk";
        }

        [TestMethod]
        public void ValidEmailAddressWillSatisfiyTheEmailValidationSpecification()
        {
            Assert.IsTrue(Customer.EmailRegex().IsMatch(_validEmailAddress));
        }
    }
}