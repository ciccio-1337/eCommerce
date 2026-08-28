using Microsoft.VisualStudio.TestTools.UnitTesting;
using eCommerce.Storefront.Model.Customers;
using eCommerce.Storefront.Model;

namespace eCommerce.Storefront.Tests.AddressSpecs
{
    [TestClass]
    public class WhenCreatingNewAddressWithBlankZipCode
    {
        [TestMethod]
        public void ThenAnInvalidAddressExceptionWillBeThrown()
        {
            var invalidAddress = new DeliveryAddress()
            {
                AddressLine = "99 Old street",
                City = "City",
                State = "State",
                Country = "Country",
                ZipCode = string.Empty,
                Name = "Home",
                Customer = new Customer()
            };

            Assert.Throws<EntityBaseIsInvalidException>(invalidAddress.ThrowExceptionIfInvalid);
        }
    }
}