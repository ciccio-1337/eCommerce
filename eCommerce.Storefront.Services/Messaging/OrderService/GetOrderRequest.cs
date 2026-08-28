namespace eCommerce.Storefront.Services.Messaging.OrderService
{
    public class GetOrderRequest
    {
        public long OrderId { get; set; }
        public string CustomerEmail { get; set; }
    }
}