using System.Linq;
using eCommerce.Storefront.Model.Products;

namespace eCommerce.Storefront.Services.Cache.Specifications
{
    public class ProductIsInBrandSpecification(long[] brandIds) : IProductSearchSpecification
    {
        private readonly long[] _brandIds = brandIds;

        public bool IsSatisfiedBy(Product product)
        {
            if (_brandIds != null && _brandIds.Length > 0)
            {
                return product?.Brand != null && _brandIds.Contains(product.Brand.Id);
            }

            return true;
        }
    }
}