namespace eCommerce.Storefront.Services.ViewModels
{
    public class OrderItemView
    {
        public string ProductName { get; set; }
        public string ProductSizeName { get; set; }
        public long Id { get; set; }
        public int Qty { get; set; }
        public string Price { get; set; }
    }
}