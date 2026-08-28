using eCommerce.Storefront.Model.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eCommerce.Storefront.Tests.CustomerSpecs
{
    [TestClass]
    public class WhenAddingValidAddressToCustomer : WithValidCustomer
    {
        private DeliveryAddress _address;

        public override void When()
        {
            _address = new DeliveryAddress()
            {
                AddressLine = "99 Old street",
                City = "City",
                State = "State",
                Country = "Country",
                ZipCode = "PostCode",
                Name = "My Work Pad",
                Customer = Customer
            };

            Customer.AddAddress(_address);
        }

        [TestMethod]
        public void ThenTheAddressShouldAppearInTheCustomersList()
        {
            Assert.HasCount(1, Customer.DeliveryAddressBook);
            Assert.Contains(_address, Customer.DeliveryAddressBook);
        }
    }
}