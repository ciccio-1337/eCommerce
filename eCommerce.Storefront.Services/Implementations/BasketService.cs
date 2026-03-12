using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using eCommerce.Storefront.Model.Basket;
using eCommerce.Storefront.Model.Shipping;
using eCommerce.Storefront.Services.Interfaces;
using eCommerce.Storefront.Services.Messaging.ProductCatalogService;
using eCommerce.Storefront.Services.ViewModels;
using eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Interfaces;
using eCommerce.Storefront.Repository.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Storefront.Services.Implementations
{
    public class BasketService : IBasketService
    {
        private readonly IBasketRepository _basketRepository;
        private readonly IProductRepository _productRepository;
        private readonly IDeliveryOptionRepository _deliveryOptionRepository;
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ICustomerRepository _customerRepository;

        public BasketService(IBasketRepository basketRepository,
            IProductRepository productRepository,
            IDeliveryOptionRepository deliveryOptionRepository,
            IUnitOfWork uow,
            IMapper mapper,
            ICustomerRepository customerRepository)
        {
            _basketRepository = basketRepository;
            _productRepository = productRepository;
            _deliveryOptionRepository = deliveryOptionRepository;
            _uow = uow;
            _mapper = mapper;
            _customerRepository = customerRepository;
        }
        
        public async Task<GetBasketResponse> GetBasketAsync(GetBasketRequest basketRequest)
        {
            var response = new GetBasketResponse();
            var basket = await _basketRepository.FindByAsync(basketRequest.BasketId);

            BasketView basketView;
            
            if (basket != null)
            {
                basketView = _mapper.Map<Basket, BasketView>(basket);
            }
            else
            {
                basketView = new BasketView();
            }

            response.Basket = basketView;
            
            return response;
        }

        public async Task<CreateBasketResponse> CreateBasketAsync(CreateBasketRequest basketRequest)
        {
            var response = new CreateBasketResponse();
            var basket = new Basket();
            var customer = await _customerRepository.FindByAsync(basketRequest.CustomerEmail);

            customer.Email = basketRequest.CustomerEmail;

            basket.SetDeliveryOption(await GetCheapestDeliveryOptionAsync());
            await AddProductsToBasketAsync(basketRequest.ProductsToAdd, basket);
            basket.SetCustomer(customer);
            basket.ThrowExceptionIfInvalid();
            _basketRepository.Save(basket);
            customer.AddBasket(basket);
            customer.ThrowExceptionIfInvalid();
            _customerRepository.Save(customer);
            await _uow.CommitAsync();
            
            response.Basket = _mapper.Map<Basket, BasketView>(basket);
            
            return response;
        }
        
        private async Task<DeliveryOption> GetCheapestDeliveryOptionAsync()
        {
            return await _deliveryOptionRepository.FindAll().OrderBy(d => d.Cost).FirstOrDefaultAsync();
        }

        public async Task<ModifyBasketResponse> ModifyBasketAsync(ModifyBasketRequest request)
        {
            var response = new ModifyBasketResponse();
            var basket = await _basketRepository.FindByAsync(request.BasketId);

            if (basket == null)
            {
                throw new BasketDoesNotExistException();
            }
            
            await AddProductsToBasketAsync(request.ProductsToAdd, basket);
            await UpdateLineQtysAsync(request.ItemsToUpdate, basket);
            await RemoveItemsFromBasketAsync(request.ItemsToRemove, basket);
            
            if (request.SetShippingServiceIdTo != 0)
            {
                var deliveryOption =await _deliveryOptionRepository.FindByAsync(request.SetShippingServiceIdTo);
                
                basket.SetDeliveryOption(deliveryOption);
            }

            basket.ThrowExceptionIfInvalid();
            _basketRepository.Save(basket);
            await _uow.CommitAsync();

            response.Basket = _mapper.Map<Basket, BasketView>(basket);
            
            return response;
        }
        
        private async Task RemoveItemsFromBasketAsync(IList<long> productsToRemove, Basket basket)
        {
            foreach (int productId in productsToRemove)
            {
                var product = await _productRepository.FindByAsync(productId);

                if (product != null)
                {
                    basket.Remove(product);
                }
            }
        }

        private async Task UpdateLineQtysAsync(IList<ProductQtyUpdateRequest> productQtyUpdateRequests, Basket basket)
        {
            foreach (ProductQtyUpdateRequest productQtyUpdateRequest in productQtyUpdateRequests)
            {
                var product = await _productRepository.FindByAsync(productQtyUpdateRequest.ProductId);
                
                if (product != null)
                {
                    basket.ChangeQtyOfProduct(productQtyUpdateRequest.NewQty, product);
                }
            }
        }

        private async Task AddProductsToBasketAsync(IList<long> productsToAdd, Basket basket)
        {
            if (productsToAdd.Any())
            {
                foreach (int productId in productsToAdd)
                {
                    var product = await _productRepository.FindByAsync(productId);

                    basket.Add(product);
                }
            }
        }

        public GetAllDispatchOptionsResponse GetAllDispatchOptions()
        {
            var response = new GetAllDispatchOptionsResponse
            {
                DeliveryOptions = _deliveryOptionRepository.FindAll().OrderBy(d => d.Cost).Select(d => _mapper.Map<DeliveryOption, DeliveryOptionView>(d))
            };

            return response;
        }
    }
}