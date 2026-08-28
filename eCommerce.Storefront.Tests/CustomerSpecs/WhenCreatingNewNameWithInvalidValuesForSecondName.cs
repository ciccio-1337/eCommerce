using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eCommerce.Storefront.Tests.CustomerSpecs
{
    [TestClass]
    public class WhenCreatingNewNameWithInvalidValuesForSecondName : WithValidCustomer
    {
        private dynamic _newName;

        public override void When()
        {
            _newName = new
            {
                FirstName = "Mickey",
                SecondName = string.Empty
            };

            Customer.FirstName = _newName.FirstName;
            Customer.SecondName = _newName.SecondName;
        }

        [TestMethod]
        public void ThenTheCustomerShouldHaveOneBrokenRule()
        {
            Assert.HasCount(1, Customer.GetBrokenRules());
        }
    }
}