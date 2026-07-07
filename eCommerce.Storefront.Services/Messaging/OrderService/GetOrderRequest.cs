namespace eCommerce.Storefront.Services.Messaging.OrderService
{
    public class GetOrderRequest
    {
        public int OrderId { get; set; }
        public string CustomerEmail { get; set; }
    }
}