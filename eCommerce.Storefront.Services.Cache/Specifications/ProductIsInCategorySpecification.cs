using eCommerce.Storefront.Model.Products;

namespace eCommerce.Storefront.Services.Cache.Specifications
{
    public class ProductIsInCategorySpecification(long categoryId) : IProductSearchSpecification
    {
        private readonly long _categoryId = categoryId;

        public bool IsSatisfiedBy(Product product)
        {
            return product?.Category?.Id == _categoryId;
        }
    }
}