using eCommerce.Storefront.Model;
using eCommerce.Storefront.Model.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eCommerce.Storefront.Tests.AddressSpecs
{
    [TestClass]
    public class WhenCreatingNewAddressWithBlankContry
    {
        [TestMethod]
        public void ThenAnInvalidAddressExceptionWillBeThrown()
        {
            var invalidAddress = new DeliveryAddress()
            {
                AddressLine = "99 Old street",
                City = "City",
                State = "State",
                Country = string.Empty,
                ZipCode = "PostCode",
                Name = "Home",
                Customer = new Customer()
            };

            Assert.Throws<EntityBaseIsInvalidException>(invalidAddress.ThrowExceptionIfInvalid);
        }
    }
}