namespace eCommerce.Storefront.Model.Customers
{
    public class DeliveryAddress : EntityBase<long>
    {
        // Keep in sync with DeliveryAddressConfiguration (all columns max 50).
        public const int MaxFieldLength = 50;

        public string AddressLine { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string ZipCode { get; set; }
        public string Name { get; set; }
        public Customer Customer { get; set; }

        protected override void Validate()
        {    
            ValidateRequired(nameof(AddressLine), AddressLine, "The line of a delivery address is required.");
            ValidateRequired(nameof(City), City, "A delivery address must have a city.");
            ValidateRequired(nameof(Country), Country, "A delivery address must have a country.");
            ValidateRequired(nameof(ZipCode), ZipCode, "A delivery address must have a zip code.");
            ValidateRequired(nameof(Name), Name, "A delivery address must have a name.");
            ValidateLength(nameof(AddressLine), AddressLine);
            ValidateLength(nameof(City), City);
            ValidateLength(nameof(State), State);
            ValidateLength(nameof(Country), Country);
            ValidateLength(nameof(ZipCode), ZipCode);
            ValidateLength(nameof(Name), Name);

            if (Customer == null)
            {
                AddBrokenRule(new BusinessRule() { Property = nameof(Customer), Rule = "A delivery address must be associated with a customer." });
            }
        }

        private void ValidateRequired(string property, string value, string message)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                AddBrokenRule(new BusinessRule() { Property = property, Rule = message });
            }
        }

        private void ValidateLength(string property, string value)
        {
            if (value != null && value.Length > MaxFieldLength)
            {
                AddBrokenRule(new BusinessRule() { Property = property, Rule = $"{property} must be at most {MaxFieldLength} characters." });
            }
        }
    }
}
