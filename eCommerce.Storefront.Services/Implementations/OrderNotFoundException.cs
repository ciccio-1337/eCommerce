using System;

namespace eCommerce.Storefront.Services.Implementations
{
    public class OrderNotFoundException(long orderId) : Exception($"Order with id '{orderId}' was not found.")
    {
    }
}
