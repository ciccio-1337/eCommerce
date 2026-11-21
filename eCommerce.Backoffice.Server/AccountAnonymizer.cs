using System;
using eCommerce.Storefront.Model.Customers;

namespace eCommerce.Backoffice.Server
{
    /// <summary>
    /// Keeps financial history while removing the personal data of a deleted
    /// account. Orders reference the customer row, so a customer that has orders
    /// cannot be deleted without destroying them; anonymising the row preserves
    /// the orders and their audit trail.
    /// </summary>
    public static class AccountAnonymizer
    {
        public const string DeletedFirstName = "Deleted";
        public const string DeletedSecondName = "Customer";

        public static bool HasOrders(Customer customer)
        {
            ArgumentNullException.ThrowIfNull(customer);

            return customer.Orders.Count > 0;
        }

        public static void Anonymize(Customer customer)
        {
            ArgumentNullException.ThrowIfNull(customer);

            customer.FirstName = DeletedFirstName;
            customer.SecondName = DeletedSecondName;
            // Rotate the linkage to the (now deleted) Identity user so the profile
            // is no longer reachable by, or correlated with, the old account.
            customer.UserId = $"deleted-{Guid.NewGuid():N}";
        }
    }
}
