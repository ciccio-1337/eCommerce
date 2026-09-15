using System;

namespace eCommerce.Storefront.Services.Implementations
{
    public class DeliveryOptionNotFoundException(long deliveryOptionId) : Exception($"The delivery option '{deliveryOptionId}' does not exist.")
    {
    }
}