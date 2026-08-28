namespace eCommerce.Storefront.Services.Messaging.ProductCatalogService
{
    public class GetProductsByCategoryRequest
    {
        public GetProductsByCategoryRequest()
        {
            ColorIds = [];
            BrandIds = [];
            SizeIds = [];
        }

        public int CategoryId { get; set; }
        public int[] ColorIds { get; set; }
        public int[] BrandIds { get; set; }
        public int[] SizeIds { get; set; }
        public ProductsSortBy SortBy { get; set; }
        public int Index { get; set; }
        public int NumberOfResultsPerPage { get; set; }
    }
}