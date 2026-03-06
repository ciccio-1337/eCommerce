using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eCommerce.Storefront.Tests.CustomerSpecs
{
    [TestClass]
    public class WhenCreatingNewNameWithInvalidValuesForFirstName : WithValidCustomer
    {
        private dynamic _newName;

        public override void When()
        {
            _newName = new 
            {
                FirstName = string.Empty,
                SecondName = "Mouse"
            };

            sut.FirstName = _newName.FirstName;
            sut.SecondName = _newName.SecondName;
        }

        [TestMethod]
        public void ThenTheCustomerShouldHaveOneBrokenRule()
        {
            Assert.AreEqual(1, sut.GetBrokenRules().Count());
        }
    }
}