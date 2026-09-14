using System.Linq;
using eCommerce.Storefront.Model.Products;

namespace eCommerce.Storefront.Services.Cache.Specifications
{
    public class ProductIsInSizeSpecification(long[] sizeIds) : IProductSearchSpecification
    {
        private readonly long[] _sizeIds = sizeIds;

        public bool IsSatisfiedBy(Product product)
        {
            if (_sizeIds != null && _sizeIds.Length > 0)
            {
                if (product?.Size != null)
                {
                    return _sizeIds.Contains(product.Size.Id);
                }

                if (product?.Title?.Products != null)
                {
                    return _sizeIds.Any(s => product.Title.Products.Any(p => p?.Size?.Id == s));
                }

                return false;
            }

            return true;
        }
    }
}