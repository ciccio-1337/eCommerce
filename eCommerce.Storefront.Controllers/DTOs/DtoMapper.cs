using System.Collections.Generic;
using eCommerce.Storefront.Services.Messaging.ProductCatalogService;

namespace eCommerce.Storefront.Controllers.DTOs
{
    public static class DtoMapper
    {
        public static IList<ProductQtyUpdateRequest> ConvertToBasketItemUpdateRequests(this BasketQtyUpdateRequest basketQtyUpdateRequest)
        {
            return basketQtyUpdateRequest.Items.ConvertToBasketItemUpdateRequests();
        }

        public static IList<ProductQtyUpdateRequest> ConvertToBasketItemUpdateRequests(this BasketItemUpdateRequest[] basketItemUpdateRequests)
        {
            var productQtyUpdateRequest = new List<ProductQtyUpdateRequest>();

            for (int i = 0; i < basketItemUpdateRequests.Length; i++)
            {
                productQtyUpdateRequest.Add(basketItemUpdateRequests[i].ConvertToBasketItemUpdateRequest());
            }

            return productQtyUpdateRequest;
        }

        public static ProductQtyUpdateRequest ConvertToBasketItemUpdateRequest(this BasketItemUpdateRequest basketItemUpdateRequest)
        {
            return new ProductQtyUpdateRequest
            {
                ProductId = basketItemUpdateRequest.ProductId,
                NewQty = basketItemUpdateRequest.Qty
            };
        }
    }
}