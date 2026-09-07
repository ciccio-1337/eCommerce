using System;
using System.Collections.Generic;

namespace eCommerce.Storefront.Services.Messaging.ProductCatalogService
{
    public class ModifyBasketRequest
    {
        public ModifyBasketRequest()
        {
            ItemsToRemove = [];
            ProductsToAdd = [];
            ItemsToUpdate = [];
        }

        public Guid BasketId { get; set; }
        public IList<long> ItemsToRemove { get; set; }
        public IList<ProductQtyUpdateRequest> ItemsToUpdate { get; set; }
        public long SetShippingServiceIdTo { get; set; }
        public IList<long> ProductsToAdd { get; set; }
    }
}