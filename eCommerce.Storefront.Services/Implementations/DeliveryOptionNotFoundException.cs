using System;

namespace eCommerce.Storefront.Services.Implementations
{
    public class DeliveryOptionNotFoundException(long deliveryOptionId) : Exception($"The delivery option '{deliveryOptionId}' does not exist.")
    {
        // Used when a basket has no delivery option at all (the optional FK is null),
        // e.g. every delivery option was deleted or no shipping options were seeded.
        public DeliveryOptionNotFoundException() : this(0)
        {
        }
    }
}