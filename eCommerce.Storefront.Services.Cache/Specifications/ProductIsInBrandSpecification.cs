using System.Linq;
using eCommerce.Storefront.Model.Products;

namespace eCommerce.Storefront.Services.Cache.Specifications
{
    public class ProductIsInBrandSpecification(int[] brandIds) : IProductSearchSpecification
    {
        private readonly int[] _brandIds = brandIds;

        public bool IsSatisfiedBy(Product product)
        {
            if (_brandIds.Length > 0)
            {
                return _brandIds.Any(b => b == product.Title.Brand.Id);
            }

            return true;
        }
    }
}