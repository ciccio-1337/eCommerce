using eCommerce.Storefront.Model.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eCommerce.Storefront.Tests.AddressSpecs
{
    [TestClass]
    public class WhenCreatingNewAddressWithBlankState
    {
        [TestMethod]
        public void ThenAnInvalidAddressExceptionWillNotBeThrown()
        {
            var validAddress = new DeliveryAddress()
            {
                AddressLine = "99 Old street",
                City = "City",
                State = string.Empty,
                Country = "Country",
                ZipCode = "PostCode",
                Name = "Home",
                Customer = new Customer()
            };

            validAddress.ThrowExceptionIfInvalid();
        }
    }
}