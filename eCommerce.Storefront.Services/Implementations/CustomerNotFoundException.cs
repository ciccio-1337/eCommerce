using System;

namespace eCommerce.Storefront.Services.Implementations
{
    public class CustomerNotFoundException(string email) : Exception($"Customer with email '{email}' was not found.")
    {
    }
}
