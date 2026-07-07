using System;

namespace eCommerce.Storefront.Services.Implementations
{
    public class CustomerNotFoundException : Exception
    {
        public CustomerNotFoundException(string email)
            : base($"Customer with email '{email}' was not found.")
        {
        }
    }
}
