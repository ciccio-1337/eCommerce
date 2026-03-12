using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using eCommerce.Storefront.Model.Products;
using eCommerce.Storefront.Services.Messaging.ProductCatalogService;
using LinqKit;

namespace eCommerce.Storefront.Services.Implementations
{
    public static class ProductSearchRequestQueryGenerator
    {
        public static Expression<Func<Product, bool>> CreateQueryFor(GetProductsByCategoryRequest getProductsByCategoryRequest)
        {
            Expression<Func<Product, bool>> productQuery = null;
            Expression<Func<Product, bool>> categoryQuery = null;

            var colorQuery = new List<Expression<Func<Product, bool>>>();
            var brandQuery = new List<Expression<Func<Product, bool>>>();
            var sizeQuery = new List<Expression<Func<Product, bool>>>();

            foreach (int id in getProductsByCategoryRequest.ColorIds)
            {
                colorQuery.Add(p => p.Title.Color.Id == id);
            }

            if (colorQuery.Count > 0)
            {
                foreach (var predicate in colorQuery)
                {
                    if (productQuery == null)
                    {
                        productQuery = predicate;
                    }
                    else
                    {
                        productQuery = PredicateBuilder.Or(productQuery, predicate);
                    }
                }
            }

            foreach (var id in getProductsByCategoryRequest.BrandIds)
            {
                brandQuery.Add(p => p.Title.Brand.Id == id);
            }

            if (brandQuery.Count > 0)
            {
                foreach (var predicate in brandQuery)
                {
                    if (productQuery == null)
                    {
                        productQuery = predicate;
                    }
                    else
                    {
                        productQuery = PredicateBuilder.Or(productQuery, predicate);
                    }
                }
            }

            foreach (var id in getProductsByCategoryRequest.SizeIds)
            {
                sizeQuery.Add(p => p.Size.Id == id);
            }

            if (sizeQuery.Count > 0)
            {
                foreach (var predicate in sizeQuery)
                {
                    if (productQuery == null)
                    {
                        productQuery = predicate;
                    }
                    else
                    {
                        productQuery = PredicateBuilder.Or(productQuery, predicate);
                    }
                }
            }

            categoryQuery = p => p.Title.Category.Id == getProductsByCategoryRequest.CategoryId;
            
            if (productQuery == null)
            {
                productQuery = categoryQuery;
            }
            else
            {
                productQuery = PredicateBuilder.Or(productQuery, categoryQuery);
            }

            return productQuery;
        }
    }
}