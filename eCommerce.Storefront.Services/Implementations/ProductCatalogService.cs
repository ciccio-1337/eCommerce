using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using eCommerce.Storefront.Model.Products;
using eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Interfaces;
using eCommerce.Storefront.Services.Interfaces;
using eCommerce.Storefront.Services.Messaging.ProductCatalogService;
using eCommerce.Storefront.Services.ViewModels;

namespace eCommerce.Storefront.Services.Implementations
{
    public class ProductCatalogService : IProductCatalogService
    {
        private readonly IProductTitleRepository _productTitleRepository;
        private readonly IProductRepository _productRepository;
        private readonly IReadOnlyRepository<Category, long> _categoryRepository;
        private readonly IMapper _mapper;

        public ProductCatalogService(IProductTitleRepository productTitleRepository,
            IProductRepository productRepository,
            IReadOnlyRepository<Category, long> categoryRepository,
            IMapper mapper)
        {
            _productTitleRepository = productTitleRepository;
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _mapper = mapper;
        }

        public GetAllCategoriesResponse GetAllCategories()
        {
            var response = new GetAllCategoriesResponse
            {
                Categories = _categoryRepository.FindAll().Select(c => _mapper.Map<Category, CategoryView>(c))
            };

            return response;
        }

        public GetFeaturedProductsResponse GetFeaturedProducts()
        {
            var response = new GetFeaturedProductsResponse
            {
                Products = _productTitleRepository.FindAll().OrderByDescending(p => p.Price).ThenBy(p => p.Brand.Name).ThenBy(p => p.Name).Take(6).Select(p => _mapper.Map<ProductTitle, ProductSummaryView>(p))
            };

            return response;
        }

        public async Task<GetProductResponse> GetProductAsync(GetProductRequest request)
        {
            var response = new GetProductResponse();
            var productTitle = await _productTitleRepository.FindByAsync(request.ProductId);            

            response.Product = _mapper.Map<ProductTitle, ProductView>(productTitle);

            return response;
        }

        public async Task<GetProductsByCategoryResponse> GetProductsByCategoryAsync(GetProductsByCategoryRequest request)
        {
            var productQuery = ProductSearchRequestQueryGenerator.CreateQueryFor(request);
            var productsMatchingRefinement = GetAllProductsMatchingQueryAndSort(request, productQuery);
            var response = CreateProductSearchResultFrom(productsMatchingRefinement, request);

            response.SelectedCategoryName = (await _categoryRepository.FindByAsync(request.CategoryId)).Name;

            return response;
        }

        private IEnumerable<Product> GetAllProductsMatchingQueryAndSort(GetProductsByCategoryRequest request, Expression<Func<Product, bool>> productQuery)
        {
            var productsMatchingRefinement = _productRepository.FindBy(productQuery);

            switch (request.SortBy)
            {
                case ProductsSortBy.PriceLowToHigh:
                    productsMatchingRefinement = productsMatchingRefinement.OrderBy(p => p.Price).ThenBy(p => p.Brand.Name).ThenBy(p => p.Name);
                    
                    break;
                case ProductsSortBy.PriceHighToLow:
                    productsMatchingRefinement = productsMatchingRefinement.OrderByDescending(p => p.Price).ThenBy(p => p.Brand.Name).ThenBy(p => p.Name);
                    
                    break;
            }
            
            return productsMatchingRefinement;
        }

        public GetProductsByCategoryResponse CreateProductSearchResultFrom(IEnumerable<Product> productsMatchingRefinement, GetProductsByCategoryRequest request)
        {
            var productSearchResultView = new GetProductsByCategoryResponse();
            var productsFound = productsMatchingRefinement.Select(p => p.Title);

            productSearchResultView.SelectedCategory = request.CategoryId;
            productSearchResultView.NumberOfTitlesFound = productsFound.GroupBy(t => t.Id).Select(g => g.First()).Count();
            productSearchResultView.TotalNumberOfPages = NoOfResultPagesGiven(request.NumberOfResultsPerPage, productSearchResultView.NumberOfTitlesFound);
            productSearchResultView.RefinementGroups = GenerateAvailableProductRefinementsFrom(productsFound);
            productSearchResultView.Products = CropProductListToSatisfyGivenIndex(request.Index, request.NumberOfResultsPerPage, productsFound);

            return productSearchResultView;
        }

        private IEnumerable<ProductSummaryView> CropProductListToSatisfyGivenIndex(int pageIndex, int numberOfResultsPerPage, IEnumerable<ProductTitle> productsFound)
        {
            if (pageIndex > 1)
            {
                var numToSkip = (pageIndex - 1) * numberOfResultsPerPage;

                return _mapper.Map<IEnumerable<ProductTitle>, IEnumerable<ProductSummaryView>>(productsFound.GroupBy(t => t.Id).Select(g => g.First()).Skip(numToSkip).Take(numberOfResultsPerPage));
            }
            else
            {
                return _mapper.Map<IEnumerable<ProductTitle>, IEnumerable<ProductSummaryView>>(productsFound.GroupBy(t => t.Id).Select(g => g.First()).Take(numberOfResultsPerPage));
            }
        }

        private int NoOfResultPagesGiven(int numberOfResultsPerPage, int numberOfTitlesFound)
        {
            if (numberOfTitlesFound < numberOfResultsPerPage)
            {
                return 1;
            }
            else
            {
                return (numberOfTitlesFound / numberOfResultsPerPage) + (numberOfTitlesFound % numberOfResultsPerPage);
            }
        }

        private IList<RefinementGroup> GenerateAvailableProductRefinementsFrom(IEnumerable<ProductTitle> productsFound)
        {
            var brandsRefinementGroup = ConvertToRefinementGroup(productsFound.SelectMany(p => p.Products).Select(p => p.Brand).GroupBy(b => b.Id).Select(g => g.First()).ToList().ConvertAll(b => (IProductAttribute)b), RefinementGroupings.Brand);
            var colorsRefinementGroup = ConvertToRefinementGroup(productsFound.SelectMany(p => p.Products).Select(p => p.Color).GroupBy(c => c.Id).Select(g => g.First()).ToList().ConvertAll(c => (IProductAttribute)c), RefinementGroupings.Color);
            var sizesRefinementGroup = ConvertToRefinementGroup(productsFound.SelectMany(p => p.Products).Select(p => p.Size).GroupBy(s => s.Id).Select(g => g.First()).ToList().ConvertAll(s => (IProductAttribute)s), RefinementGroupings.Size);
            var refinementGroups = new List<RefinementGroup>
            {
                brandsRefinementGroup,
                colorsRefinementGroup,
                sizesRefinementGroup
            };
            
            return refinementGroups;
        }

        private RefinementGroup ConvertToRefinementGroup(IEnumerable<IProductAttribute> productAttributes, RefinementGroupings refinementGroupType)
        {
            var refinementGroup = new RefinementGroup
            {
                Name = refinementGroupType.ToString(),
                GroupId = (int)refinementGroupType,
                Refinements = _mapper.Map<IEnumerable<IProductAttribute>, IEnumerable<Refinement>>(productAttributes)
            };

            return refinementGroup;
        }
    }
}