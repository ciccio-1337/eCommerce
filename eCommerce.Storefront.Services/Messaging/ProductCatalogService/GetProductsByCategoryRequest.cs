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

        public long CategoryId { get; set; }
        public long[] ColorIds { get; set; }
        public long[] BrandIds { get; set; }
        public long[] SizeIds { get; set; }
        public ProductsSortBy SortBy { get; set; }
        public int Index { get; set; }
        public int NumberOfResultsPerPage { get; set; }
    }
}