#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using MapsterMapper;
using eCommerce.Storefront.Model.Products;
using eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Interfaces;
using eCommerce.Storefront.Services.Interfaces;
using eCommerce.Storefront.Services.Messaging.ProductCatalogService;
using eCommerce.Storefront.Services.ViewModels;

namespace eCommerce.Storefront.Services.Cache
{
    public class CachedProductCatalogService(ICacheStorage cacheStorage,
        IProductCatalogService productCatalogService,
        IProductTitleRepository productTitleRepository,
        IMapper mapper) : ICachedProductCatalogService
    {
        private readonly ICacheStorage _cacheStorage = cacheStorage;
        private readonly IProductCatalogService _productCatalogService = productCatalogService;
        private readonly IProductTitleRepository _productTitleRepository = productTitleRepository;
        // The service is registered scoped, so instance locks would give every request
        // its own lock and provide no cache-stampede protection. Static locks are shared
        // by all instances (the underlying IMemoryCache is already shared).
        private static readonly Lock GetTopSellingProductsLock = new();
        private static readonly Lock GetAllProductTitlesLock = new();
        private static readonly Lock GetAllCategoriesLock = new();
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

        private IReadOnlyDictionary<long, ProductView> FindAllProductViews()
        {
            lock (GetAllProductTitlesLock)
            {
                if (!_cacheStorage.TryRetrieve(CacheKeys.AllProductTitles.ToString(), out IReadOnlyDictionary<long, ProductView>? productViews))
                {
                    // Map while filling the cache: only detached DTOs are retained.
                    // Caching the EF entities would keep their navigation graphs alive
                    // and let a later request see entities tracked by this request's
                    // scoped DbContext.
                    productViews = _productTitleRepository.FindAll()
                        .AsEnumerable()
                        .Select(title => _mapper.Map<ProductTitle, ProductView>(title))
                        .ToDictionary(view => view.Id);

                    _cacheStorage.Store(CacheKeys.AllProductTitles.ToString(), productViews);
                }

                return productViews ?? EmptyProductViews;
            }
        }

        private static readonly IReadOnlyDictionary<long, ProductView> EmptyProductViews =
            new Dictionary<long, ProductView>();

        public GetFeaturedProductsResponse GetFeaturedProducts()
        {
            lock (GetTopSellingProductsLock)
            {
                var response = new GetFeaturedProductsResponse();

                if (!_cacheStorage.TryRetrieve(CacheKeys.TopSellingProducts.ToString(), out IReadOnlyList<ProductSummaryView>? productViews))
                {
                    response = _productCatalogService.GetFeaturedProducts();

                    // Cache a deep, immutable snapshot of the view models.
                    _cacheStorage.Store(CacheKeys.TopSellingProducts.ToString(),
                        response.Products.Select(CloneProductSummary).ToList().AsReadOnly());
                }
                else
                {
                    // Deep-copy the cached views: returning the cached instances would
                    // let a caller that mutates one corrupt the shared cache.
                    // 'productViews' is guaranteed non-null when TryRetrieve returned true.
                    response.Products = productViews!.Select(CloneProductSummary).ToList();
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

        public Task<GetProductResponse> GetProductAsync(GetProductRequest request)
        {
            var productViews = FindAllProductViews();
            // Explicit lookup: an unknown id yields null, exactly like the uncached
            // SQL path. Returning a deep copy keeps callers from mutating the cache.
            var response = new GetProductResponse
            {
                Product = productViews.TryGetValue(request.ProductId, out var product)
                    ? CloneProduct(product)
                    : null
            };

            return Task.FromResult(response);
        }

        public GetAllCategoriesResponse GetAllCategories()
        {
            lock (GetAllCategoriesLock)
            {
                if (!_cacheStorage.TryRetrieve(CacheKeys.AllCategories.ToString(), out GetAllCategoriesResponse? response))
                {
                    response = _productCatalogService.GetAllCategories();
                    // Store an immutable, deep-copied snapshot.
                    response.Categories = response.Categories?.Select(CloneCategory).ToArray() ?? [];

                    _cacheStorage.Store(CacheKeys.AllCategories.ToString(), response);
                }

                // Return a fresh response with fresh CategoryView instances so
                // callers mutating either the array or an item don't affect cache.
                return new GetAllCategoriesResponse
                {
                    // 'response' is non-null here: TryRetrieve returned true.
                    Categories = response!.Categories?.Select(CloneCategory).ToArray() ?? []
                };
            }
        }

        public GetProductsByCategoryResponse CreateProductSearchResultFrom(IEnumerable<Product> productsMatchingRefinement, GetProductsByCategoryRequest request)
        {
            return _productCatalogService.CreateProductSearchResultFrom(productsMatchingRefinement, request);
        }

        private static ProductSummaryView CloneProductSummary(ProductSummaryView product) => new()
        {
            Id = product.Id,
            BrandName = product.BrandName,
            Name = product.Name,
            Price = product.Price
        };

        private static ProductView CloneProduct(ProductView product) => new()
        {
            Id = product.Id,
            BrandName = product.BrandName,
            Name = product.Name,
            Price = product.Price,
            Products = product.Products?.Select(size => new ProductSizeOption
            {
                Id = size.Id,
                SizeName = size.SizeName
            }).ToList() ?? []
        };

        private static CategoryView CloneCategory(CategoryView category) => new()
        {
            Id = category.Id,
            Name = category.Name
        };
    }
}
