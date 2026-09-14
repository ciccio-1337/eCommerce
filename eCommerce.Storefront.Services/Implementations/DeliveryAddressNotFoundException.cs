using System;

namespace eCommerce.Storefront.Services.Implementations
{
    public class DeliveryAddressNotFoundException(long addressId) : Exception($"Delivery address with id '{addressId}' was not found.")
    {
    }
}