namespace eCommerce.Storefront.Services.Messaging.ProductCatalogService
{
    public class ProductQtyUpdateRequest
    {
        public long ProductId { get; set; }
        public int NewQty { get; set; }
    }
}