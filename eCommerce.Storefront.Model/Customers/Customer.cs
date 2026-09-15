using System;
using System.Collections.Generic;
using eCommerce.Storefront.Model.Orders;
using System.Text.RegularExpressions;

namespace eCommerce.Storefront.Model.Customers
{
    public partial class Customer : EntityBase<long>
    {
        [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
        public static partial Regex EmailRegex();

        private readonly IList<DeliveryAddress> _deliveryAddressBook = [];
        private Basket.Basket _basket;

        public string UserId { get; set; }
        public string FirstName { get; set; }
        public string SecondName { get; set; }
        public string Email { get; set; }
        // Getter-only so callers cannot swap the whole collection (which would
        // make EF treat previously-loaded orders as removed). EF Core populates
        // the collection items; a public setter here would also make it easy to
        // accidentally orphan orders via replacement.
        public List<Order> Orders { get; } = [];

        public void AddAddress(DeliveryAddress deliveryAddress)
        {
            ArgumentNullException.ThrowIfNull(deliveryAddress);
            deliveryAddress.ThrowExceptionIfInvalid();
            _deliveryAddressBook.Add(deliveryAddress);
        }

        // Read-only view over the address book; additions go through AddAddress.
        // EF Core fix-up writes to the backing field.
        public IReadOnlyList<DeliveryAddress> DeliveryAddressBook
        {
            get { return _deliveryAddressBook.AsReadOnly(); }
        }

        public void AddBasket(Basket.Basket basket)
        {
            basket.ThrowExceptionIfInvalid();

            _basket = basket;
        }

        public Basket.Basket Basket
        {
            get { return _basket; }
        }

        protected override void Validate()
        {
            if (string.IsNullOrWhiteSpace(FirstName))
            {
                AddBrokenRule(new BusinessRule() { Property = nameof(FirstName), Rule = "A customer must have a first name." });
            }

            if (string.IsNullOrWhiteSpace(SecondName))
            {
                AddBrokenRule(new BusinessRule() { Property = nameof(SecondName), Rule = "A customer must have a second name." });
            }

            if (!EmailRegex().IsMatch(Email ?? string.Empty))
            {
                AddBrokenRule(new BusinessRule() { Property = nameof(Email), Rule = "A customer must have a valid email address." });
            }

            if (string.IsNullOrWhiteSpace(UserId))
            {
                AddBrokenRule(new BusinessRule() { Property = nameof(UserId), Rule = "A customer must have an identity user." });
            }
        }
    }
}
