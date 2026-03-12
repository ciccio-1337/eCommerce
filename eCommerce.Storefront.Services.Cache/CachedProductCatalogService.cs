using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using eCommerce.Storefront.Model.Products;
using eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Interfaces;
using eCommerce.Storefront.Services.Cache.Specifications;
using eCommerce.Storefront.Services.Interfaces;
using eCommerce.Storefront.Services.Messaging.ProductCatalogService;
using eCommerce.Storefront.Services.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Storefront.Services.Cache
{
    public class CachedProductCatalogService : ICachedProductCatalogService
    {
        private readonly ICacheStorage _cacheStorage;
        private readonly IProductCatalogService _productCatalogService;
        private readonly IProductTitleRepository _productTitleRepository;
        private readonly IProductRepository _productRepository;
        private readonly object _getTopSellingProductsLock;
        private readonly SemaphoreSlim _getAllProductTitlesLock;
        private readonly SemaphoreSlim _getAllProductsLock;
        private readonly object _getAllCategoriesLock;
        private readonly IMapper _mapper;

        public CachedProductCatalogService(ICacheStorage cacheStorage,
            IProductCatalogService productCatalogService,
            IProductTitleRepository productTitleRepository,
            IProductRepository productRepository,
            IMapper mapper)
        {
            _cacheStorage = cacheStorage;
            _productCatalogService = productCatalogService;
            _productTitleRepository = productTitleRepository;
            _productRepository = productRepository;
            _getTopSellingProductsLock = new object();
            _getAllProductTitlesLock = new SemaphoreSlim(1, 1);
            _getAllProductsLock = new SemaphoreSlim(1, 1);
            _getAllCategoriesLock = new object();
            _mapper = mapper;
        }

        private async Task<IEnumerable<ProductTitle>> FindAllProductTitlesAsync()
        {
            await _getAllProductTitlesLock.WaitAsync();

            try
            {
                var allProductTitles = _cacheStorage.Retrieve<IEnumerable<ProductTitle>>(CacheKeys.AllProductTitles.ToString());

                if (allProductTitles == null)
                {
                    allProductTitles = await _productTitleRepository.FindAll().ToListAsync();

                    _cacheStorage.Store(CacheKeys.AllProductTitles.ToString(), allProductTitles);
                }

                return allProductTitles;
            }
            finally
            {
                _getAllProductTitlesLock.Release();
            }
        }

        private async Task<IEnumerable<Product>> FindAllProductsAsync()
        {
            await _getAllProductsLock.WaitAsync();

            try
            {
                var allProducts = _cacheStorage.Retrieve<IEnumerable<Product>>(CacheKeys.AllProducts.ToString());

                if (allProducts == null)
                {
                    allProducts = await _productRepository.FindAll().ToListAsync();

                    _cacheStorage.Store(CacheKeys.AllProducts.ToString(), allProducts);
                }

                return allProducts;
            }
            finally
            {
                _getAllProductsLock.Release();
            }
        }

        public GetFeaturedProductsResponse GetFeaturedProducts()
        {
            lock (_getTopSellingProductsLock)
            {
                var response = new GetFeaturedProductsResponse();
                var productViews = _cacheStorage.Retrieve<IEnumerable<ProductSummaryView>>(CacheKeys.TopSellingProducts.ToString());

                if (productViews == null)
                {
                    response = _productCatalogService.GetFeaturedProducts();

                    _cacheStorage.Store(CacheKeys.TopSellingProducts.ToString(), response.Products.ToList());
                }
                else
                {
                    response.Products = productViews;
                }

                return response;
            }
        }

        public async Task<GetProductsByCategoryResponse> GetProductsByCategoryAsync(GetProductsByCategoryRequest request)
        {
            var colourSpecification = new ProductIsInColorSpecification(request.ColorIds);
            var brandSpecification = new ProductIsInBrandSpecification(request.BrandIds);
            var sizeSpecification = new ProductIsInSizeSpecification(request.SizeIds);
            var categorySpecification = new ProductIsInCategorySpecification(request.CategoryId);
            var matchingProducts = (await FindAllProductsAsync()).Where(colourSpecification.IsSatisfiedBy)
                .Where(brandSpecification.IsSatisfiedBy)
                .Where(sizeSpecification.IsSatisfiedBy)
                .Where(categorySpecification.IsSatisfiedBy);

            switch (request.SortBy)
            {
                case ProductsSortBy.PriceLowToHigh:
                    matchingProducts = matchingProducts.OrderBy(p => p.Price).ThenBy(p => p.Brand.Name).ThenBy(p => p.Name);

                    break;
                case ProductsSortBy.PriceHighToLow:
                    matchingProducts = matchingProducts.OrderByDescending(p => p.Price).ThenBy(p => p.Brand.Name).ThenBy(p => p.Name);

                    break;
            }

            var response = CreateProductSearchResultFrom(matchingProducts, request);

            response.SelectedCategoryName = GetAllCategories().Categories.FirstOrDefault(c => c.Id == request.CategoryId)?.Name;                                

            return response;
        }
        
        public async Task<GetProductResponse> GetProductAsync(GetProductRequest request)
        {
            var response = new GetProductResponse
            {
                Product = _mapper.Map<ProductTitle, ProductView>((await FindAllProductTitlesAsync()).FirstOrDefault(p => p.Id == request.ProductId))
            };

            return response;
        }

        public GetAllCategoriesResponse GetAllCategories()
        {
            lock (_getAllCategoriesLock)
            {
                var response = _cacheStorage.Retrieve<GetAllCategoriesResponse>(CacheKeys.AllCategories.ToString());

                if (response == null)
                {
                    response = _productCatalogService.GetAllCategories();
                    response.Categories = response.Categories.ToList();
                    
                    _cacheStorage.Store(CacheKeys.AllCategories.ToString(), response);
                }

                return response;
            }
        }

        public GetProductsByCategoryResponse CreateProductSearchResultFrom(IEnumerable<Product> productsMatchingRefinement, GetProductsByCategoryRequest request)
        {
            return _productCatalogService.CreateProductSearchResultFrom(productsMatchingRefinement, request);
        }
    }
}
