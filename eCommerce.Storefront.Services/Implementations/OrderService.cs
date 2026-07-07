using System.Linq;
using AutoMapper;
using eCommerce.Storefront.Model.Basket;
using eCommerce.Storefront.Model.Orders;
using eCommerce.Storefront.Services.Interfaces;
using eCommerce.Storefront.Services.Messaging.OrderService;
using eCommerce.Storefront.Services.ViewModels;
using System;
using System.Text;
using eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Interfaces;
using eCommerce.Storefront.Repository.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace eCommerce.Storefront.Services.Implementations
{
    public class OrderService : IOrderService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IBasketRepository _basketRepository;
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<OrderService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public OrderService(IOrderRepository orderRepository,
            IBasketRepository basketRepository,
            ICustomerRepository customerRepository,
            IUnitOfWork uow,
            IMapper mapper,
            ILogger<OrderService> logger,
            IConfiguration configuration,
            IEmailService emailService)
        {
            _customerRepository = customerRepository;
            _orderRepository = orderRepository;
            _basketRepository = basketRepository;
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
            _configuration = configuration;
            _emailService = emailService;
        }

        public async Task<CreateOrderResponse> CreateOrderAsync(CreateOrderRequest request)
        {
            var response = new CreateOrderResponse();
            var customer = await _customerRepository.FindByAsync(request.CustomerEmail);
            var basket = await _basketRepository.FindByAsync(request.BasketId);

            if (customer == null)
            {
                throw new CustomerNotFoundException(request.CustomerEmail);
            }

            if (basket == null)
            {
                throw new BasketDoesNotExistException();
            }

            var deliveryAddress = customer.DeliveryAddressBook.FirstOrDefault(d => d.Id == request.DeliveryId);
            var order = ConvertToOrder(basket);

            order.Customer = customer;
            order.DeliveryAddress = deliveryAddress;

            order.ThrowExceptionIfInvalid();
            _orderRepository.Save(order);
            _basketRepository.Remove(basket);
            await _uow.CommitAsync();

            response.Order = _mapper.Map<Order, OrderView>(order);

            return response;
        }

        public async Task<SetOrderPaymentResponse> SetOrderPaymentAsync(SetOrderPaymentRequest paymentRequest)
        {
            var paymentResponse = new SetOrderPaymentResponse();
            var order = await _orderRepository.FindByAsync(paymentRequest.OrderId);

            if (order == null)
            {
                throw new OrderNotFoundException(paymentRequest.OrderId);
            }

            try
            {
                order.SetPayment(new Payment(DateTime.Now, paymentRequest.PaymentToken, paymentRequest.PaymentMerchant, paymentRequest.Amount));
                await SubmitAsync(order, paymentRequest.CustomerEmail);
                _orderRepository.Save(order);
                await _uow.CommitAsync();
            }
            catch (OrderAlreadyPaidForException ex)
            {
                // Refund the payment using the payment service.
                _logger.LogError(ex, "Order {OrderId} has already been paid for. Refund required.", order.Id);
            }
            catch (PaymentAmountDoesNotEqualOrderTotalException ex)
            {
                // Refund the payment using the payment service.
                _logger.LogError(ex, "Payment amount mismatch for order {OrderId}. Refund required.", order.Id);
            }

            paymentResponse.Order = _mapper.Map<Order, OrderView>(order);

            return paymentResponse;
        }

        public async Task<GetOrderResponse> GetOrderAsync(GetOrderRequest request)
        {
            var response = new GetOrderResponse();
            var order = await _orderRepository.FindByAsync(request.OrderId);

            if (order == null || order.Customer == null ||
                !string.Equals(order.Customer.Email, request.CustomerEmail, StringComparison.OrdinalIgnoreCase))
            {
                return response;
            }

            response.Order = _mapper.Map<Order, OrderView>(order);

            return response;
        }

        private Order ConvertToOrder(Basket basket)
        {
            var order = new Order
            {
                ShippingCharge = basket.DeliveryCost(),
                ShippingService = basket.DeliveryOption.ShippingService
            };

            foreach (BasketItem item in basket.Items)
            {
                order.AddItem(item.Product, item.Qty);
            }

            return order;
        }

        private async Task SubmitAsync(Order order, string customerEmail)
        {
            if (order.Status == OrderStatus.Open)
            {
                if (order.OrderHasBeenPaidFor())
                {
                    order.Status = OrderStatus.Submitted;
                }

                var emailBody = new StringBuilder();
                var emailAddress = customerEmail;
                var emailSubject = string.Format("Order #{0}", order.Id);

                emailBody.AppendLine(string.Format("Hello {0},", order.Customer.FirstName));
                emailBody.AppendLine();
                emailBody.AppendLine("The following order will be packed and dispatched as soon as possible.");
                emailBody.AppendLine(order.ToString());
                emailBody.AppendLine();
                emailBody.AppendLine("Thank you for your custom.");

                if (!string.IsNullOrWhiteSpace(_configuration["MailSettingsSmtpNetworkPassword"]))
                {
                    try
                    {
                        await _emailService.SendMailAsync(_configuration["MailSettingsSmtpNetworkUserName"], emailAddress, emailSubject, emailBody.ToString());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send order confirmation email for order {OrderId}.", order.Id);
                    }
                }
            }
            else
            {
                throw new InvalidOperationException("You cannot submit this order as it has already been submitted.");
            }
        }
    }
}