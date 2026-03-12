using System;
using eCommerce.Storefront.Controllers.DTOs;
using eCommerce.Storefront.Controllers.ViewModels;
using eCommerce.Storefront.Controllers.ViewModels.ProductCatalog;
using eCommerce.Storefront.Services.Implementations;
using eCommerce.Storefront.Services.Interfaces;
using eCommerce.Storefront.Services.Messaging.ProductCatalogService;
using Microsoft.AspNetCore.Mvc;
using eCommerce.Storefront.Services.Cache;
using Microsoft.AspNetCore.Authorization;
using eCommerce.Storefront.Controllers.Services.Interfaces;
using System.Threading.Tasks;

namespace eCommerce.Storefront.Controllers.Controllers
{
    [Authorize(Roles = "Customer")]
    public class BasketController : ProductCatalogBaseController
    {
        private readonly IBasketService _basketService;

        public BasketController(ICachedProductCatalogService cachedProductCatalogService,
            IBasketService basketService,
            ICookieAuthentication cookieAuthentication,
            ICustomerService customerService) : base(cookieAuthentication, 
                customerService,
                cachedProductCatalogService)
        {
            _basketService = basketService;
        }

        public async Task<IActionResult> Detail()
        {
            var basketView = new BasketDetailView();
            var basketId = await GetBasketIdAsync();
            var basketRequest = new GetBasketRequest() 
            { 
                BasketId = basketId 
            };
            var basketResponse = await _basketService.GetBasketAsync(basketRequest);
            var dispatchOptionsResponse = _basketService.GetAllDispatchOptions();

            basketView.Basket = basketResponse.Basket;
            basketView.Categories = GetCategories();
            basketView.BasketSummary = await GetBasketSummaryViewAsync();
            basketView.DeliveryOptions = dispatchOptionsResponse.DeliveryOptions;
            
            return View("View", basketView);
        }

        [HttpPost]
        public async Task<IActionResult> RemoveItem(int productId)
        {
            var request = new ModifyBasketRequest();

            request.ItemsToRemove.Add(productId);

            request.BasketId = await GetBasketIdAsync();
            
            var response = await _basketService.ModifyBasketAsync(request);
            var basketDetailView = new BasketDetailView
            {
                BasketSummary = new BasketSummaryView
                {
                    BasketTotal = response.Basket.BasketTotal,
                    NumberOfItems = response.Basket.NumberOfItems
                },
                Basket = response.Basket,
                DeliveryOptions = _basketService.GetAllDispatchOptions().DeliveryOptions
            };

            return Ok(basketDetailView);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateShipping(int shippingServiceId)
        {
            var request = new ModifyBasketRequest
            {
                SetShippingServiceIdTo = shippingServiceId,
                BasketId = await GetBasketIdAsync()
            };
            var basketDetailView = new BasketDetailView();
            var response = await _basketService.ModifyBasketAsync(request);

            basketDetailView.BasketSummary = new BasketSummaryView
            {
                BasketTotal = response.Basket.BasketTotal,
                NumberOfItems = response.Basket.NumberOfItems
            };
            basketDetailView.Basket = response.Basket;
            basketDetailView.DeliveryOptions = _basketService.GetAllDispatchOptions().DeliveryOptions;
            
            return Ok(basketDetailView);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateItems([FromBody] BasketQtyUpdateRequest jsonBasketQtyUpdateRequest)
        {
            var request = new ModifyBasketRequest
            {
                BasketId = await GetBasketIdAsync(),
                ItemsToUpdate = jsonBasketQtyUpdateRequest.ConvertToBasketItemUpdateRequests()
            };
            var basketDetailView = new BasketDetailView();
            var response = await _basketService.ModifyBasketAsync(request);

            basketDetailView.BasketSummary = new BasketSummaryView
            {
                BasketTotal = response.Basket.BasketTotal,
                NumberOfItems = response.Basket.NumberOfItems
            };
            basketDetailView.Basket = response.Basket;
            basketDetailView.DeliveryOptions = _basketService.GetAllDispatchOptions().DeliveryOptions;
            
            return Ok(basketDetailView);
        }

        [HttpPost]
        public async Task<IActionResult> AddToBasket(int productId)
        {
            var basketSummaryView = new BasketSummaryView();
            var basketId = await GetBasketIdAsync();
            var createNewBasket = basketId == Guid.Empty;

            if (!createNewBasket)
            {
                var modifyBasketRequest = new ModifyBasketRequest();

                modifyBasketRequest.ProductsToAdd.Add(productId);

                modifyBasketRequest.BasketId = basketId;
                
                try
                {
                    var response = await _basketService.ModifyBasketAsync(modifyBasketRequest);

                    basketSummaryView = response.Basket.ConvertToSummary();
                }
                catch (BasketDoesNotExistException)
                {
                    createNewBasket = true;
                }
            }

            if (createNewBasket)
            {
                var createBasketRequest = new CreateBasketRequest
                {
                    CustomerEmail = _cookieAuthentication.GetAuthenticationToken()
                };

                createBasketRequest.ProductsToAdd.Add(productId);

                var response = await _basketService.CreateBasketAsync(createBasketRequest);

                basketSummaryView = response.Basket.ConvertToSummary();
            }

            return Ok(basketSummaryView);
        }
    }
}