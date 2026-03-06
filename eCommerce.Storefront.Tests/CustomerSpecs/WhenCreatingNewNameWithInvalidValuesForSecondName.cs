using System.Linq;
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