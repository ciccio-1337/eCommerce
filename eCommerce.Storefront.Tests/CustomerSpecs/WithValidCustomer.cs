using System;
using eCommerce.Storefront.Model.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eCommerce.Storefront.Tests.CustomerSpecs
{
    public abstract class WithValidCustomer
    {
        protected Customer Customer { get; set; } = null;

        [TestInitialize]
        public void Context()
        {
            Customer = new Customer()
            {
                FirstName = "Francesco",
                SecondName = "Guagnano",
                Email = "francescoguagnano@alice.it",
                UserId = Guid.NewGuid().ToString()
            };

            When();
        }

        public abstract void When();
    }
}