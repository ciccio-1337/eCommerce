using System.Collections.Generic;
using eCommerce.Storefront.Services.Messaging.ProductCatalogService;

namespace eCommerce.Storefront.Controllers.DTOs
{
    public class ProductSearchRequest
    {
        public long CategoryId { get; set; }
        public long[] ColorIds { get; set; }
        public long[] SizeIds { get; set; }
        public long[] BrandIds { get; set; }
        public ProductsSortBy SortBy { get; set; }
        public IEnumerable<RefinementGroup> RefinementGroups { get; set; }
        public int Index { get; set; }
    }
}