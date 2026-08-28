namespace eCommerce.Storefront.Services.Messaging.OrderService
{
    public class SetOrderPaymentRequest
    {
        public string PaymentToken { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMerchant { get; set; }
        public long OrderId { get; set; }
        public string CustomerEmail { get; set; }
    }
}