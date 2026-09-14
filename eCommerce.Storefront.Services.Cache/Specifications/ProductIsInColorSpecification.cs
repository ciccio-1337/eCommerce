using System.Linq;
using eCommerce.Storefront.Model.Products;

namespace eCommerce.Storefront.Services.Cache.Specifications
{
    public class ProductIsInColorSpecification(long[] colourIds) : IProductSearchSpecification
    {
        private readonly long[] _colourIds = colourIds;

        public bool IsSatisfiedBy(Product product)
        {
            if (_colourIds != null && _colourIds.Length > 0)
            {
                return product?.Color != null && _colourIds.Contains(product.Color.Id);
            }

            return true;
        }
    }
}