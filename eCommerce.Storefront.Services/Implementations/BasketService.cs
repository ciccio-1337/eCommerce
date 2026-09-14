using System.Collections.Generic;
using System.Linq;
using MapsterMapper;
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
    public class BasketService(IBasketRepository basketRepository,
        IProductRepository productRepository,
        IDeliveryOptionRepository deliveryOptionRepository,
        IUnitOfWork uow,
        IMapper mapper,
        ICustomerRepository customerRepository) : IBasketService
    {
        private readonly IBasketRepository _basketRepository = basketRepository;
        private readonly IProductRepository _productRepository = productRepository;
        private readonly IDeliveryOptionRepository _deliveryOptionRepository = deliveryOptionRepository;
        private readonly IUnitOfWork _uow = uow;
        private readonly IMapper _mapper = mapper;
        private readonly ICustomerRepository _customerRepository = customerRepository;

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
            var customer = await _customerRepository.FindByAsync(basketRequest.CustomerEmail) ??
                throw new CustomerNotFoundException(basketRequest.CustomerEmail);

            // A customer may only have one basket; never orphan an existing basket by
            // creating and persisting a new empty one alongside it.
            if (customer.Basket != null)
            {
                throw new BasketAlreadyExistsException();
            }

            customer.Email = basketRequest.CustomerEmail;

            var basket = new Basket();
            
            basket.SetDeliveryOption(await GetCheapestDeliveryOptionAsync());
            await AddProductsToBasketAsync(basketRequest.ProductsToAdd, basket);

            // Never persist an empty basket that has no products in it.
            if (!basket.Items.Any())
            {
                response.Basket = _mapper.Map<Basket, BasketView>(basket);

                return response;
            }

            basket.SetCustomer(customer);
            basket.ThrowExceptionIfInvalid();
            await _basketRepository.AddAsync(basket);
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
            var basket = await _basketRepository.FindByAsync(request.BasketId) ??
                throw new BasketDoesNotExistException();

            await AddProductsToBasketAsync(request.ProductsToAdd, basket);
            await UpdateLineQtysAsync(request.ItemsToUpdate, basket);
            await RemoveItemsFromBasketAsync(request.ItemsToRemove, basket);

            if (request.SetShippingServiceIdTo != 0)
            {
                var deliveryOption = await _deliveryOptionRepository.FindByAsync(request.SetShippingServiceIdTo);

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
            if (!productsToRemove.Any())
            {
                return;
            }

            var products = await _productRepository.FindBy(c => productsToRemove.Contains(c.Id)).ToListAsync();
            var productById = products.ToDictionary(p => p.Id);

            foreach (long productId in productsToRemove)
            {
                if (productById.TryGetValue(productId, out var product))
                {
                    basket.Remove(product);
                }
            }
        }

        private async Task UpdateLineQtysAsync(IList<ProductQtyUpdateRequest> productQtyUpdateRequests, Basket basket)
        {
            if (!productQtyUpdateRequests.Any())
            {
                return;
            }

            var productIds = productQtyUpdateRequests.Select(r => r.ProductId).ToList();
            var products = await _productRepository.FindBy(c => productIds.Contains(c.Id)).ToListAsync();
            var productById = products.ToDictionary(p => p.Id);

            foreach (ProductQtyUpdateRequest productQtyUpdateRequest in productQtyUpdateRequests)
            {
                if (productById.TryGetValue(productQtyUpdateRequest.ProductId, out var product))
                {
                    // A qty of 0 (or less) from the basket UI means the line should be
                    // removed. BasketItem.ChangeItemQtyTo rejects non-positive quantities,
                    // so a zero/negative entry can never represent a valid line.
                    if (productQtyUpdateRequest.NewQty <= 0)
                    {
                        basket.Remove(product);
                    }
                    else
                    {
                        basket.ChangeQtyOfProduct(productQtyUpdateRequest.NewQty, product);
                    }
                }
            }
        }

        private async Task AddProductsToBasketAsync(IList<long> productsToAdd, Basket basket)
        {
            if (!productsToAdd.Any())
            {
                return;
            }

            var products = await _productRepository.FindBy(c => productsToAdd.Contains(c.Id)).ToListAsync();
            var productById = products.ToDictionary(p => p.Id);

            foreach (long productId in productsToAdd)
            {
                if (productById.TryGetValue(productId, out var product))
                {
                    basket.Add(product);
                }
            }
        }

        public GetAllDispatchOptionsResponse GetAllDispatchOptions()
        {
            var response = new GetAllDispatchOptionsResponse
            {
                DeliveryOptions = [.. _deliveryOptionRepository.FindAll().OrderBy(d => d.Cost).AsEnumerable().Select(_mapper.Map<DeliveryOption, DeliveryOptionView>)]
            };

            return response;
        }
    }
}