namespace eCommerce.Storefront.Controllers.DTOs
{
    public class BasketItemUpdateRequest
    {
        public long ProductId { get; set; }
        public int Qty { get; set; }
    }
}