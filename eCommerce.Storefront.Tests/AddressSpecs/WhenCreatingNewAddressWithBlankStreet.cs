using Microsoft.VisualStudio.TestTools.UnitTesting;
using eCommerce.Storefront.Model.Customers;
using eCommerce.Storefront.Model;

namespace eCommerce.Storefront.Tests.AddressSpecs
{
    [TestClass]
    public class WhenCreatingNewAddressWithBlankStreet
    {
        [TestMethod]
        public void ThenAnInvalidAddressExceptionWillBeThrown()
        {
            Assert.Throws<EntityBaseIsInvalidException>(() =>
            {                
                DeliveryAddress invalidAddress = new DeliveryAddress()
                {
                    AddressLine = string.Empty, 
                    City = "City", 
                    State = "State", 
                    Country = "Country",
                    ZipCode = "PostCode",
                    Name = "Home",
                    Customer = new Customer()
                };

                invalidAddress.ThrowExceptionIfInvalid();
            });
        }
    }
}