using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using eCommerce.Storefront.Model.Customers;
using eCommerce.Storefront.Model.Orders;
using eCommerce.Storefront.Services.Interfaces;
using eCommerce.Storefront.Services.Messaging.CustomerService;
using eCommerce.Storefront.Services.ViewModels;
using eCommerce.Storefront.Model.Basket;
using eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Interfaces;
using eCommerce.Storefront.Repository.EntityFrameworkCore;
using System.Threading.Tasks;

namespace eCommerce.Storefront.Services.Implementations
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public CustomerService(ICustomerRepository customerRepository,
            IUnitOfWork uow,
            IMapper mapper)
        {
            _customerRepository = customerRepository;
            _uow = uow;
            _mapper = mapper;
        }

        public async Task<CreateCustomerResponse> CreateCustomerAsync(CreateCustomerRequest request)
        {
            var response = new CreateCustomerResponse();
            var customer = new Customer
            {
                UserId = request.UserId,
                Email = request.Email,
                FirstName = request.FirstName,
                SecondName = request.SecondName
            };

            customer.ThrowExceptionIfInvalid();
            await _customerRepository.AddAsync(customer);
            await _uow.CommitAsync();

            response.Customer = _mapper.Map<Customer, CustomerView>(customer);

            return response;
        }

        public async Task<GetCustomerResponse> GetCustomerAsync(GetCustomerRequest request)
        {
            var response = new GetCustomerResponse();
            var customer = await _customerRepository.FindByAsync(request.CustomerEmail);

            if (customer != null)
            {
                response.CustomerFound = true;
                response.Customer = _mapper.Map<Customer, CustomerView>(customer);
                response.Customer.Email = request.CustomerEmail;
                
                if (request.LoadOrderSummary)
                {
                    response.Orders = _mapper.Map<IEnumerable<Order>, IEnumerable<OrderSummaryView>>(customer.Orders.OrderByDescending(o => o.Created));
                }

                if (request.LoadBasketSummary)
                {
                    response.Basket = _mapper.Map<Basket, BasketView>(customer.Basket);
                }
            }
            else
            {
                response.CustomerFound = false;
            }

            return response;
        }

        public async Task<ModifyCustomerResponse> ModifyCustomerAsync(ModifyCustomerRequest request)
        {
            var response = new ModifyCustomerResponse();
            var customer = await _customerRepository.FindByAsync(request.CurrentEmail);

            customer.FirstName = request.FirstName;
            customer.SecondName = request.SecondName;
            customer.Email = request.NewEmail;

            customer.ThrowExceptionIfInvalid();
            _customerRepository.Save(customer);
            await _customerRepository.SaveEmailAsync(customer.UserId, customer.Email);
            await _uow.CommitAsync();

            response.Customer = _mapper.Map<Customer, CustomerView>(customer);

            return response;
        }

        public async Task<DeliveryAddressModifyResponse> ModifyDeliveryAddressAsync(DeliveryAddressModifyRequest request)
        {
            var response = new DeliveryAddressModifyResponse();
            var customer = await _customerRepository.FindByAsync(request.CustomerEmail);
            var deliveryAddress = customer.DeliveryAddressBook.FirstOrDefault(d => d.Id == request.Address.Id);

            if (deliveryAddress != null)
            {
                UpdateDeliveryAddressFrom(request.Address, deliveryAddress);
                _customerRepository.Save(customer);
                await _uow.CommitAsync();
            }

            response.DeliveryAddress = _mapper.Map<DeliveryAddress, DeliveryAddressView>(deliveryAddress);

            return response;
        }

        public async Task<DeliveryAddressAddResponse> AddDeliveryAddressAsync(DeliveryAddressAddRequest request)
        {
            var response = new DeliveryAddressAddResponse();
            var customer = await _customerRepository.FindByAsync(request.CustomerEmail);
            var deliveryAddress = new DeliveryAddress
            {
                Customer = customer
            };

            UpdateDeliveryAddressFrom(request.Address, deliveryAddress);
            customer.AddAddress(deliveryAddress);
            _customerRepository.Save(customer);
            await _uow.CommitAsync();

            response.DeliveryAddress = _mapper.Map<DeliveryAddress, DeliveryAddressView>(deliveryAddress);

            return response;
        }

        private void UpdateDeliveryAddressFrom(DeliveryAddressView deliveryAddressSource, DeliveryAddress deliveryAddressToUpdate)
        {
            deliveryAddressToUpdate.Name = deliveryAddressSource.Name;
            deliveryAddressToUpdate.AddressLine = deliveryAddressSource.AddressLine;
            deliveryAddressToUpdate.City = deliveryAddressSource.City;
            deliveryAddressToUpdate.State = deliveryAddressSource.State;
            deliveryAddressToUpdate.Country = deliveryAddressSource.Country;
            deliveryAddressToUpdate.ZipCode = deliveryAddressSource.ZipCode;

            deliveryAddressToUpdate.ThrowExceptionIfInvalid();
        }
    }
}