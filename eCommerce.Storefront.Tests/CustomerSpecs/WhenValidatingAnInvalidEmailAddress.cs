using eCommerce.Storefront.Model.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eCommerce.Storefront.Tests.CustomerSpecs
{
    [TestClass]
    public class WhenValidatingAnInvalidEmailAddress
    {
        private string _invalidEmailAddress;

        [TestInitialize]
        public void Given()
        {
            _invalidEmailAddress = "gg@kkkkk";
        }

        [TestMethod]
        public void ThenTheEmailAddressWillNotSatisfiyTheEmailValidationSpecification()
        {
            Assert.IsFalse(Customer.EmailRegex().IsMatch(_invalidEmailAddress));
        }
    }
}