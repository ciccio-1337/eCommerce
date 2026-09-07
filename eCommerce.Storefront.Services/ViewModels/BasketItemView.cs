namespace eCommerce.Storefront.Services.ViewModels
{
    public class BasketItemView
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductSizeName { get; set; }
        public long ProductTitleId { get; set; }
        public int Qty { get; set; }
        public string ProductPrice { get; set; }
        public string LineTotal { get; set; }
    }
}