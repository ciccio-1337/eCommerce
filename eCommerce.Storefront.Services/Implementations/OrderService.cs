using System.Linq;
using MapsterMapper;
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
    public class OrderService(IOrderRepository orderRepository,
        IBasketRepository basketRepository,
        ICustomerRepository customerRepository,
        IUnitOfWork uow,
        IMapper mapper,
        ILogger<OrderService> logger,
        IConfiguration configuration,
        IEmailService emailService) : IOrderService
    {
        private readonly ICustomerRepository _customerRepository = customerRepository;
        private readonly IOrderRepository _orderRepository = orderRepository;
        private readonly IBasketRepository _basketRepository = basketRepository;
        private readonly IUnitOfWork _uow = uow;
        private readonly IMapper _mapper = mapper;
        private readonly ILogger<OrderService> _logger = logger;
        private readonly IConfiguration _configuration = configuration;
        private readonly IEmailService _emailService = emailService;

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
            await _orderRepository.AddAsync(order);
            _basketRepository.Remove(basket);
            await _uow.CommitAsync();

            response.Order = _mapper.Map<Order, OrderView>(order);

            return response;
        }

        public async Task<SetOrderPaymentResponse> SetOrderPaymentAsync(SetOrderPaymentRequest paymentRequest)
        {
            var paymentResponse = new SetOrderPaymentResponse();
            var order = await _orderRepository.FindByAsync(paymentRequest.OrderId) ??
                throw new OrderNotFoundException(paymentRequest.OrderId);

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

            if (order == null || order.Customer == null)
            {
                return response;
            }

            if (!string.IsNullOrWhiteSpace(request.CustomerEmail))
            {
                var customer = await _customerRepository.FindByAsync(request.CustomerEmail);

                if (customer == null || order.Customer.Id != customer.Id)
                {
                    return response;
                }
            }

            response.Order = _mapper.Map<Order, OrderView>(order);

            return response;
        }

        private static Order ConvertToOrder(Basket basket)
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

                var smtpPassword = _configuration["MailSettings:Smtp:Network:Password"] ?? _configuration["MailSettingsSmtpNetworkPassword"];
                var smtpUserName = _configuration["MailSettings:Smtp:Network:UserName"] ?? _configuration["MailSettingsSmtpNetworkUserName"];

                if (!string.IsNullOrWhiteSpace(smtpPassword) && !string.IsNullOrWhiteSpace(emailAddress))
                {
                    try
                    {
                        await _emailService.SendMailAsync(smtpUserName, emailAddress, emailSubject, emailBody.ToString());
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