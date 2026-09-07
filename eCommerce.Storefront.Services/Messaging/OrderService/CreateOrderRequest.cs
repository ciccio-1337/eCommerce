using System;

namespace eCommerce.Storefront.Services.Messaging.OrderService
{
    public class CreateOrderRequest
    {
        public long DeliveryId { get; set; }
        public Guid BasketId { get; set; }
        public string CustomerEmail { get; set; }
    }
}