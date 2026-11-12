using eCommerce.Storefront.Model;
using eCommerce.Storefront.Model.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eCommerce.Storefront.Tests.CustomerSpecs
{
    [TestClass]
    public class WhenValidatingLengthLimits
    {
        [TestMethod]
        public void Customer_WithFirstNameAtTheLimit_IsValid()
        {
            AssertValid(CreateCustomer(firstName: new string('a', Customer.MaxNameLength)));
        }

        [TestMethod]
        public void Customer_WithOverlongFirstName_IsInvalid()
        {
            Assert.ThrowsExactly<EntityBaseIsInvalidException>(() =>
                CreateCustomer(firstName: new string('a', Customer.MaxNameLength + 1)).ThrowExceptionIfInvalid());
        }

        [TestMethod]
        public void Customer_WithOverlongSecondName_IsInvalid()
        {
            Assert.ThrowsExactly<EntityBaseIsInvalidException>(() =>
                CreateCustomer(secondName: new string('b', Customer.MaxNameLength + 1)).ThrowExceptionIfInvalid());
        }

        [TestMethod]
        public void DeliveryAddress_WithFieldsAtTheLimit_IsValid()
        {
            AssertValid(CreateAddress(city: new string('c', DeliveryAddress.MaxFieldLength)));
        }

        [TestMethod]
        public void DeliveryAddress_WithOverlongCity_IsInvalid()
        {
            Assert.ThrowsExactly<EntityBaseIsInvalidException>(() =>
                CreateAddress(city: new string('c', DeliveryAddress.MaxFieldLength + 1)).ThrowExceptionIfInvalid());
        }

        [TestMethod]
        public void DeliveryAddress_WithOverlongAddressLine_IsInvalid()
        {
            Assert.ThrowsExactly<EntityBaseIsInvalidException>(() =>
                CreateAddress(addressLine: new string('a', DeliveryAddress.MaxFieldLength + 1)).ThrowExceptionIfInvalid());
        }

        [TestMethod]
        public void DeliveryAddress_WithOverlongState_IsInvalid()
        {
            Assert.ThrowsExactly<EntityBaseIsInvalidException>(() =>
                CreateAddress(state: new string('s', DeliveryAddress.MaxFieldLength + 1)).ThrowExceptionIfInvalid());
        }

        private static void AssertValid(Customer customer) =>
            customer.ThrowExceptionIfInvalid();

        private static void AssertValid(DeliveryAddress address) =>
            address.ThrowExceptionIfInvalid();

        private static Customer CreateCustomer(string firstName = "John", string secondName = "Doe") =>
            new()
            {
                FirstName = firstName,
                SecondName = secondName,
                Email = "john.doe@example.com",
                UserId = "user-1"
            };

        private static DeliveryAddress CreateAddress(
            string addressLine = "1 Main Street",
            string city = "London",
            string state = "London") =>
            new()
            {
                AddressLine = addressLine,
                City = city,
                State = state,
                Country = "United Kingdom",
                ZipCode = "SW1A 1AA",
                Name = "John Doe",
                Customer = CreateCustomer()
            };
    }
}
