#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using MapsterMapper;
using eCommerce.Storefront.Model.Products;
using eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Interfaces;
using eCommerce.Storefront.Services.Cache.Specifications;
using eCommerce.Storefront.Services.Interfaces;
using eCommerce.Storefront.Services.Messaging.ProductCatalogService;
using eCommerce.Storefront.Services.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Storefront.Services.Cache
{
    public class CachedProductCatalogService(ICacheStorage cacheStorage,
        IProductCatalogService productCatalogService,
        IProductTitleRepository productTitleRepository,
        IProductRepository productRepository,
        IMapper mapper) : ICachedProductCatalogService
    {
        private readonly ICacheStorage _cacheStorage = cacheStorage;
        private readonly IProductCatalogService _productCatalogService = productCatalogService;
        private readonly IProductTitleRepository _productTitleRepository = productTitleRepository;
        private readonly IProductRepository _productRepository = productRepository;
        private readonly Lock _getTopSellingProductsLock = new();
        private readonly Lock _getAllProductTitlesLock = new();
        private readonly Lock _getAllProductsLock = new();
        private readonly Lock _getAllCategoriesLock = new();
        private readonly IMapper _mapper = mapper;

        // Invalidate all product-related cache entries. Called by backoffice
        // controllers after mutating brands, colors, sizes, categories, or products.
        public void InvalidateProductCaches()
        {
            _cacheStorage.Remove(CacheKeys.AllProductTitles.ToString());
            _cacheStorage.Remove(CacheKeys.AllProducts.ToString());
            _cacheStorage.Remove(CacheKeys.TopSellingProducts.ToString());
            _cacheStorage.Remove(CacheKeys.AllCategories.ToString());
        }

        private async Task<IReadOnlyList<ProductTitle>> FindAllProductTitlesAsync()
        {
            lock (_getAllProductTitlesLock)
            {
                if (!_cacheStorage.TryRetrieve(CacheKeys.AllProductTitles.ToString(), out IReadOnlyList<ProductTitle>? allProductTitles))
                {
                    // Materialize and cache an IMMUTABLE snapshot so callers cannot
                    // mutate the cached entities (ProductTitle has mutable navigations).
                    allProductTitles = _productTitleRepository.FindAll().ToList().AsReadOnly();

                    _cacheStorage.Store(CacheKeys.AllProductTitles.ToString(), allProductTitles);
                }

                return allProductTitles ?? new List<ProductTitle>().AsReadOnly();
            }
        }

        private async Task<IReadOnlyList<Product>> FindAllProductsAsync()
        {
            lock (_getAllProductsLock)
            {
                if (!_cacheStorage.TryRetrieve(CacheKeys.AllProducts.ToString(), out IReadOnlyList<Product>? allProducts))
                {
                    // Materialize and cache an IMMUTABLE snapshot.
                    allProducts = _productRepository.FindAll().ToList().AsReadOnly();

                    _cacheStorage.Store(CacheKeys.AllProducts.ToString(), allProducts);
                }

                return allProducts ?? new List<Product>().AsReadOnly();
            }
        }

        public GetFeaturedProductsResponse GetFeaturedProducts()
        {
            lock (_getTopSellingProductsLock)
            {
                var response = new GetFeaturedProductsResponse();

                if (!_cacheStorage.TryRetrieve(CacheKeys.TopSellingProducts.ToString(), out IReadOnlyList<ProductSummaryView>? productViews))
                {
                    response = _productCatalogService.GetFeaturedProducts();

                    // Cache an IMMUTABLE snapshot of the view models.
                    _cacheStorage.Store(CacheKeys.TopSellingProducts.ToString(), response.Products.ToList().AsReadOnly());
                }
                else
                {
                    // Return a NEW response object with the cached (immutable) views.
                    // Callers get their own list instance so mutations don't leak.
                    // 'productViews' is guaranteed non-null when TryRetrieve returned true.
                    response.Products = productViews!.ToList();
                }

                return response;
            }
        }

        // DELEGATE to the inner service so the SQL path (with its exact filter
        // semantics) is used. The cached AllProducts is NOT used here — this avoids
        // the divergence bug where in-memory specs differ from the EF expression tree.
        public async Task<GetProductsByCategoryResponse> GetProductsByCategoryAsync(GetProductsByCategoryRequest request)
        {
            return await _productCatalogService.GetProductsByCategoryAsync(request);
        }

        public async Task<GetProductResponse> GetProductAsync(GetProductRequest request)
        {
            var allTitles = await FindAllProductTitlesAsync();

            // Mapster yields a null ProductView for an unknown/missing title, which is
            // the same shape the uncached service produces for a non-existent product.
            var title = allTitles.FirstOrDefault(p => p.Id == request.ProductId);

            var response = new GetProductResponse
            {
                Product = _mapper.Map<ProductTitle, ProductView>(title!)
            };

            return response;
        }

        public GetAllCategoriesResponse GetAllCategories()
        {
            lock (_getAllCategoriesLock)
            {
                if (!_cacheStorage.TryRetrieve(CacheKeys.AllCategories.ToString(), out GetAllCategoriesResponse? response))
                {
                    response = _productCatalogService.GetAllCategories();
                    // Store an IMMUTABLE snapshot: new array of CategoryView.
                    response.Categories = [.. response.Categories];

                    _cacheStorage.Store(CacheKeys.AllCategories.ToString(), response);
                }

                // Return a fresh response with a fresh categories array so
                // callers mutating the array or CategoryView don't affect cache.
                return new GetAllCategoriesResponse
                {
                    // 'response' is non-null here: TryRetrieve returned true.
                    Categories = response!.Categories?.ToArray() ?? Array.Empty<CategoryView>()
                };
            }
        }

        public GetProductsByCategoryResponse CreateProductSearchResultFrom(IEnumerable<Product> productsMatchingRefinement, GetProductsByCategoryRequest request)
        {
            return _productCatalogService.CreateProductSearchResultFrom(productsMatchingRefinement, request);
        }
    }
}
#nullable restore
