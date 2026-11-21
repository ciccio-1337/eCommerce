using eCommerce.Backoffice.Server;
using eCommerce.Storefront.Model.Customers;
using eCommerce.Storefront.Model.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eCommerce.Storefront.Tests.Backoffice
{
    [TestClass]
    public class AccountAnonymizerTests
    {
        [TestMethod]
        public void Anonymize_ClearsPersonalDataAndPreservesOrders()
        {
            var customer = new Customer
            {
                UserId = "user-1",
                FirstName = "Jane",
                SecondName = "Doe",
                Email = "jane@test.com"
            };
            customer.Orders.Add(new Order());

            Assert.IsTrue(AccountAnonymizer.HasOrders(customer));

            AccountAnonymizer.Anonymize(customer);

            Assert.AreEqual(AccountAnonymizer.DeletedFirstName, customer.FirstName);
            Assert.AreEqual(AccountAnonymizer.DeletedSecondName, customer.SecondName);
            Assert.IsFalse(customer.UserId.Equals("user-1"));
            Assert.IsTrue(customer.UserId.StartsWith("deleted-"));
            // Orders are financial history and must survive account deletion.
            Assert.AreEqual(1, customer.Orders.Count);
        }

        [TestMethod]
        public void HasOrders_WhenCustomerHasNoOrders_ReturnsFalse()
        {
            var customer = new Customer
            {
                UserId = "user-1",
                FirstName = "Jane",
                SecondName = "Doe",
                Email = "jane@test.com"
            };

            Assert.IsFalse(AccountAnonymizer.HasOrders(customer));
        }
    }
}
