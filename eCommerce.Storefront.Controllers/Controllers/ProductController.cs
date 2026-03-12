using eCommerce.Storefront.Controllers.DTOs;
using eCommerce.Storefront.Controllers.ViewModels.ProductCatalog;
using eCommerce.Storefront.Services.Messaging.ProductCatalogService;
using eCommerce.Storefront.Services.ViewModels;
using Microsoft.AspNetCore.Mvc;
using eCommerce.Storefront.Services.Cache;
using eCommerce.Storefront.Services.Interfaces;
using eCommerce.Storefront.Controllers.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace eCommerce.Storefront.Controllers.Controllers
{
    public class ProductController : ProductCatalogBaseController
    {
        private readonly IConfiguration _configuration;

        public ProductController(IConfiguration configuration,
            ICookieAuthentication cookieAuthentication,
            ICustomerService customerService,
            ICachedProductCatalogService cachedProductCatalogService) : base(cookieAuthentication,
                customerService,
                cachedProductCatalogService)
        {
            _configuration = configuration;
        }

        public async Task<IActionResult> GetProductsByCategory(int categoryId)
        {
            var productSearchRequest = GenerateInitialProductSearchRequestFrom(categoryId);
            var response = await _cachedProductCatalogService.GetProductsByCategoryAsync(productSearchRequest);
            var productSearchResultView = await GetProductSearchResultViewFromAsync(response);

            return View("ProductSearchResults", productSearchResultView);
        }

        private async Task<ProductSearchResultView> GetProductSearchResultViewFromAsync(GetProductsByCategoryResponse response)
        {
            var productSearchResultView = new ProductSearchResultView
            {
                BasketSummary = await GetBasketSummaryViewAsync(),
                Categories = GetCategories(),
                CurrentPage = response.CurrentPage,
                NumberOfTitlesFound = response.NumberOfTitlesFound,
                Products = response.Products,
                RefinementGroups = response.RefinementGroups,
                SelectedCategory = response.SelectedCategory,
                SelectedCategoryName = response.SelectedCategoryName,
                TotalNumberOfPages = response.TotalNumberOfPages
            };

            return productSearchResultView;
        }

        private GetProductsByCategoryRequest GenerateInitialProductSearchRequestFrom(int categoryId)
        {
            var productSearchRequest = new GetProductsByCategoryRequest
            {
                NumberOfResultsPerPage = int.Parse(_configuration["NumberOfResultsPerPage"]),
                CategoryId = categoryId,
                Index = 1,
                SortBy = ProductsSortBy.PriceHighToLow
            };

            return productSearchRequest;
        }

        [HttpPost]
        public async Task<IActionResult> GetProducts([FromBody] ProductSearchRequest jsonProductSearchRequest)
        {
            var productSearchRequest = GenerateProductSearchRequestFrom(jsonProductSearchRequest);
            var response = await _cachedProductCatalogService.GetProductsByCategoryAsync(productSearchRequest);
            var productSearchResultView = await GetProductSearchResultViewFromAsync(response);

            return Ok(productSearchResultView);
        }

        private GetProductsByCategoryRequest GenerateProductSearchRequestFrom(ProductSearchRequest jsonProductSearchRequest)
        {
            var productSearchRequest = new GetProductsByCategoryRequest
            {
                NumberOfResultsPerPage = int.Parse(_configuration["NumberOfResultsPerPage"])
            };

            if (jsonProductSearchRequest != null)
            {
                productSearchRequest.Index = jsonProductSearchRequest.Index;
                productSearchRequest.CategoryId = jsonProductSearchRequest.CategoryId;
                productSearchRequest.SortBy = jsonProductSearchRequest.SortBy;

                foreach (var jsonRefinementGroup in jsonProductSearchRequest.RefinementGroups)
                {
                    switch ((RefinementGroupings)jsonRefinementGroup.GroupId)
                    {
                        case RefinementGroupings.Brand:
                            productSearchRequest.BrandIds = jsonRefinementGroup.SelectedRefinements;

                            break;
                        case RefinementGroupings.Color:
                            productSearchRequest.ColorIds = jsonRefinementGroup.SelectedRefinements;

                            break;
                        case RefinementGroupings.Size:
                            productSearchRequest.SizeIds = jsonRefinementGroup.SelectedRefinements;

                            break;
                        default:
                            break;
                    }
                }
            }

            return productSearchRequest;
        }

        public async Task<IActionResult> Detail(int id)
        {
            var productDetailView = new ProductDetailView();
            var request = new GetProductRequest 
            { 
                ProductId = id 
            };
            var response = await _cachedProductCatalogService.GetProductAsync(request);
            var productView = response.Product;

            productDetailView.Product = productView;
            productDetailView.BasketSummary = await GetBasketSummaryViewAsync();
            productDetailView.Categories = GetCategories();

            return View(productDetailView);
        }
    }
}