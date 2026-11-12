using System;
using System.Linq;
using System.Linq.Expressions;
using eCommerce.Storefront.Model.Products;
using eCommerce.Storefront.Services.Messaging.ProductCatalogService;
using LinqKit;

namespace eCommerce.Storefront.Services.Implementations
{
    public static class ProductSearchRequestQueryGenerator
    {
        // Hard upper bound on the OR trees we are willing to build. The controller
        // rejects larger requests, but this keeps the service safe on its own too.
        public const int MaxRefinementIds = 50;

        public static Expression<Func<Product, bool>> CreateQueryFor(GetProductsByCategoryRequest getProductsByCategoryRequest)
        {
            Expression<Func<Product, bool>> categoryQuery = p => p.Title.Category.Id == getProductsByCategoryRequest.CategoryId;
            Expression<Func<Product, bool>> colorQuery = null;
            Expression<Func<Product, bool>> brandQuery = null;
            Expression<Func<Product, bool>> sizeQuery = null;

            if (getProductsByCategoryRequest.ColorIds != null && getProductsByCategoryRequest.ColorIds.Length > 0)
            {
                foreach (long id in getProductsByCategoryRequest.ColorIds.Take(MaxRefinementIds))
                {
                    long currentId = id;
                    Expression<Func<Product, bool>> predicate = p => p.Title.Color.Id == currentId;

                    colorQuery = colorQuery == null ? predicate : PredicateBuilder.Or(colorQuery, predicate);
                }
            }

            if (getProductsByCategoryRequest.BrandIds != null && getProductsByCategoryRequest.BrandIds.Length > 0)
            {
                foreach (var id in getProductsByCategoryRequest.BrandIds.Take(MaxRefinementIds))
                {
                    var currentId = id;
                    Expression<Func<Product, bool>> predicate = p => p.Title.Brand.Id == currentId;

                    brandQuery = brandQuery == null ? predicate : PredicateBuilder.Or(brandQuery, predicate);
                }
            }

            if (getProductsByCategoryRequest.SizeIds != null && getProductsByCategoryRequest.SizeIds.Length > 0)
            {
                foreach (var id in getProductsByCategoryRequest.SizeIds.Take(MaxRefinementIds))
                {
                    var currentId = id;
                    Expression<Func<Product, bool>> predicate = p => p.Size.Id == currentId;

                    sizeQuery = sizeQuery == null ? predicate : PredicateBuilder.Or(sizeQuery, predicate);
                }
            }

            var productQuery = categoryQuery;

            if (colorQuery != null)
            {
                productQuery = PredicateBuilder.And(productQuery, colorQuery);
            }

            if (brandQuery != null)
            {
                productQuery = PredicateBuilder.And(productQuery, brandQuery);
            }

            if (sizeQuery != null)
            {
                productQuery = PredicateBuilder.And(productQuery, sizeQuery);
            }

            return productQuery;
        }
    }
}