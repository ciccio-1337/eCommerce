using System.Collections.Generic;
using System.Threading.Tasks;
using eCommerce.Storefront.Model.Products;
using eCommerce.Storefront.Services.Messaging.ProductCatalogService;

namespace eCommerce.Storefront.Services.Interfaces
{
    public interface IProductCatalogService
    {
        GetFeaturedProductsResponse GetFeaturedProducts();
        Task<GetProductsByCategoryResponse> GetProductsByCategoryAsync(GetProductsByCategoryRequest request);
        Task<GetProductResponse> GetProductAsync(GetProductRequest request);
        GetAllCategoriesResponse GetAllCategories();
        GetProductsByCategoryResponse CreateProductSearchResultFrom(IEnumerable<Product> productsMatchingRefinement, GetProductsByCategoryRequest request);
    }
}