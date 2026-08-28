using eCommerce.Storefront.Model.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eCommerce.Storefront.Tests.CustomerSpecs
{
    [TestClass]
    public class WhenValidatingAnEmptyEmailAddress
    {
        private string _blankEmailAddress;

        [TestInitialize]
        public void Given()
        {
            _blankEmailAddress = string.Empty;
        }

        [TestMethod]
        public void ThenTheEmailAddressWillNotSatisfiyTheEmailValidationSpecification()
        {
            Assert.IsFalse(Customer.EmailRegex().IsMatch(_blankEmailAddress));
        }
    }
}