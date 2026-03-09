using eCommerce.Storefront.Controllers.DTOs;
using eCommerce.Storefront.Controllers.ViewModels.ProductCatalog;
using eCommerce.Storefront.Services.Messaging.ProductCatalogService;
using eCommerce.Storefront.Services.ViewModels;
using Microsoft.AspNetCore.Mvc;
using eCommerce.Storefront.Services.Cache;
using eCommerce.Storefront.Services.Interfaces;
using eCommerce.Storefront.Controllers.Services.Interfaces;
using Microsoft.Extensions.Configuration;

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

        public IActionResult GetProductsByCategory(int categoryId)
        {
            GetProductsByCategoryRequest productSearchRequest = GenerateInitialProductSearchRequestFrom(categoryId);
            GetProductsByCategoryResponse response = _cachedProductCatalogService.GetProductsByCategory(productSearchRequest);
            ProductSearchResultView productSearchResultView = GetProductSearchResultViewFrom(response);

            return View("ProductSearchResults", productSearchResultView);
        }

        private ProductSearchResultView GetProductSearchResultViewFrom(GetProductsByCategoryResponse response)
        {
            ProductSearchResultView productSearchResultView = new ProductSearchResultView
            {
                BasketSummary = GetBasketSummaryView(),
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
            GetProductsByCategoryRequest productSearchRequest = new GetProductsByCategoryRequest
            {
                NumberOfResultsPerPage = int.Parse(_configuration["NumberOfResultsPerPage"]),
                CategoryId = categoryId,
                Index = 1,
                SortBy = ProductsSortBy.PriceHighToLow
            };

            return productSearchRequest;
        }

        [HttpPost]
        public IActionResult GetProducts([FromBody] ProductSearchRequest jsonProductSearchRequest)
        {
            GetProductsByCategoryRequest productSearchRequest = GenerateProductSearchRequestFrom(jsonProductSearchRequest);
            GetProductsByCategoryResponse response = _cachedProductCatalogService.GetProductsByCategory(productSearchRequest);
            ProductSearchResultView productSearchResultView = GetProductSearchResultViewFrom(response);

            return Ok(productSearchResultView);
        }

        private GetProductsByCategoryRequest GenerateProductSearchRequestFrom(ProductSearchRequest jsonProductSearchRequest)
        {
            GetProductsByCategoryRequest productSearchRequest = new GetProductsByCategoryRequest
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

        public IActionResult Detail(int id)
        {
            ProductDetailView productDetailView = new ProductDetailView();
            GetProductRequest request = new GetProductRequest 
            { 
                ProductId = id 
            };
            GetProductResponse response = _cachedProductCatalogService.GetProduct(request);
            ProductView productView = response.Product;
            productDetailView.Product = productView;
            productDetailView.BasketSummary = GetBasketSummaryView();
            productDetailView.Categories = GetCategories();

            return View(productDetailView);
        }
    }
}