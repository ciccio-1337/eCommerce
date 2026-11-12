using eCommerce.Storefront.Controllers.DTOs;
using eCommerce.Storefront.Controllers.ViewModels.ProductCatalog;
using eCommerce.Storefront.Services.Messaging.ProductCatalogService;
using eCommerce.Storefront.Services.ViewModels;
using Microsoft.AspNetCore.Mvc;
using eCommerce.Storefront.Services.Cache;
using eCommerce.Storefront.Services.Interfaces;
using eCommerce.Storefront.Controllers.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace eCommerce.Storefront.Controllers.Controllers
{
    [AllowAnonymous]
    public class ProductController(IConfiguration configuration,
        ICookieAuthentication cookieAuthentication,
        ICustomerService customerService,
        ICachedProductCatalogService cachedProductCatalogService) : ProductCatalogBaseController(cookieAuthentication,
            customerService,
            cachedProductCatalogService)
    {
        // Bounds for the anonymous search endpoint. Without them a caller can
        // overflow paging arithmetic or build arbitrarily large OR expression
        // trees, turning a cheap request into CPU/memory exhaustion.
        internal const int MaxPageIndex = 10_000;
        internal const int MaxRefinementsPerGroup = 50;

        private readonly IConfiguration _configuration = configuration;

        [HttpGet]
        [EnableRateLimiting("catalog")]
        public async Task<IActionResult> GetProductsByCategory(long categoryId)
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

        private GetProductsByCategoryRequest GenerateInitialProductSearchRequestFrom(long categoryId)
        {
            var productSearchRequest = new GetProductsByCategoryRequest
            {
                NumberOfResultsPerPage = int.TryParse(_configuration["NumberOfResultsPerPage"], out var numberPerPage) ? numberPerPage : 6,
                CategoryId = categoryId,
                Index = 1,
                SortBy = ProductsSortBy.PriceHighToLow
            };

            return productSearchRequest;
        }

        [HttpPost]
        [EnableRateLimiting("catalog")]
        public async Task<IActionResult> GetProducts([FromBody] ProductSearchRequest jsonProductSearchRequest)
        {
            if (jsonProductSearchRequest != null
                && (ExceedsRefinementLimit(jsonProductSearchRequest.ColorIds)
                    || ExceedsRefinementLimit(jsonProductSearchRequest.SizeIds)
                    || ExceedsRefinementLimit(jsonProductSearchRequest.BrandIds)))
            {
                return BadRequest(new { error = $"Select at most {MaxRefinementsPerGroup} refinements per group." });
            }

            var productSearchRequest = GenerateProductSearchRequestFrom(jsonProductSearchRequest);
            var response = await _cachedProductCatalogService.GetProductsByCategoryAsync(productSearchRequest);
            var productSearchResultView = await GetProductSearchResultViewFromAsync(response);

            return Ok(productSearchResultView);
        }

        private static bool ExceedsRefinementLimit(long[] ids) =>
            ids != null && ids.Length > MaxRefinementsPerGroup;

        private GetProductsByCategoryRequest GenerateProductSearchRequestFrom(ProductSearchRequest jsonProductSearchRequest)
        {
            var productSearchRequest = new GetProductsByCategoryRequest
            {
                NumberOfResultsPerPage = int.TryParse(_configuration["NumberOfResultsPerPage"], out var numberPerPage) ? numberPerPage : 6
            };

            if (jsonProductSearchRequest != null)
            {
                // Clamp instead of trusting the client: 0/negative values used to
                // produce a negative Skip, and int.MaxValue overflowed the page
                // arithmetic. Both surfaced as 500s.
                productSearchRequest.Index = Math.Clamp(jsonProductSearchRequest.Index, 1, MaxPageIndex);
                productSearchRequest.CategoryId = jsonProductSearchRequest.CategoryId;
                productSearchRequest.SortBy = jsonProductSearchRequest.SortBy;

                if (jsonProductSearchRequest.RefinementGroups != null)
                {
                    foreach (var jsonRefinementGroup in jsonProductSearchRequest.RefinementGroups)
                    {
                        switch ((RefinementGroupings)jsonRefinementGroup.GroupId)
                        {
                            case RefinementGroupings.Brand:
                                productSearchRequest.BrandIds = LimitRefinements(jsonRefinementGroup.SelectedRefinements);

                                break;
                            case RefinementGroupings.Color:
                                productSearchRequest.ColorIds = LimitRefinements(jsonRefinementGroup.SelectedRefinements);

                                break;
                            case RefinementGroupings.Size:
                                productSearchRequest.SizeIds = LimitRefinements(jsonRefinementGroup.SelectedRefinements);

                                break;
                            default:
                                break;
                        }
                    }
                }
            }

            return productSearchRequest;
        }

        private static long[] LimitRefinements(long[] refinements) =>
            refinements?.Take(MaxRefinementsPerGroup).ToArray() ?? [];

        public async Task<IActionResult> Detail(long id)
        {
            var productDetailView = new ProductDetailView();
            var request = new GetProductRequest
            {
                ProductId = id
            };
            var response = await _cachedProductCatalogService.GetProductAsync(request);
            var productView = response?.Product;

            if (productView == null)
            {
                return NotFound();
            }

            productDetailView.Product = productView;
            productDetailView.BasketSummary = await GetBasketSummaryViewAsync();
            productDetailView.Categories = GetCategories();

            return View(productDetailView);
        }
    }
}