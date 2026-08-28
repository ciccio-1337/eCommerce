using eCommerce.Storefront.Model.Products;

namespace eCommerce.Storefront.Services.Cache.Specifications
{
    public class ProductIsInCategorySpecification(int categoryId) : IProductSearchSpecification
    {
        private readonly int _categoryId = categoryId;

        public bool IsSatisfiedBy(Product product)
        {
            return product.Category.Id == _categoryId;
        }
    }
}