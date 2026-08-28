using System.Linq;
using eCommerce.Storefront.Model.Products;

namespace eCommerce.Storefront.Services.Cache.Specifications
{
    public class ProductIsInSizeSpecification(int[] sizeIds) : IProductSearchSpecification
    {
        private readonly int[] _sizeIds = sizeIds;

        public bool IsSatisfiedBy(Product product)
        {
            if (_sizeIds.Length > 0)
            {
                return _sizeIds.Any(s => product.Title.Products.Any(p => p.Size.Id == s));
            }

            return true;
        }
    }
}