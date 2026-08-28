using System.Collections.Generic;

namespace eCommerce.Storefront.Services.Messaging.ProductCatalogService
{
    public class CreateBasketRequest
    {
        public CreateBasketRequest()
        {
            ProductsToAdd = [];
        }

        public IList<long> ProductsToAdd { get; set; }
        public string CustomerEmail { get; set; }
    }
}