using System;

namespace eCommerce.Storefront.Services.Implementations
{
    public class EmailAlreadyInUseException(string email) : Exception($"The email address '{email}' is already in use by another account.")
    {
    }
}