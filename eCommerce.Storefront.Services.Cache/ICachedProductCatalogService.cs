using eCommerce.Storefront.Services.Interfaces;

namespace eCommerce.Storefront.Services.Cache
{
    public interface ICachedProductCatalogService : IProductCatalogService
    {
        // Invalidates all product-related cache entries. Called by backoffice
        // controllers after mutating brands, colors, sizes, categories, or products.
        void InvalidateProductCaches();
    }
}