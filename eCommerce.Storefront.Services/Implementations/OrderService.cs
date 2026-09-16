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

            if (deliveryAddress == null)
            {
                // e.g. the address was deleted in another tab since checkout started.
                throw new DeliveryAddressNotFoundException(request.DeliveryId);
            }

            if (basket.DeliveryOption == null)
            {
                // Delivery options are auto-assigned the cheapest one when a basket is
                // created, but the relationship is an optional FK — a basket can reach
                // checkout without one if every option was deleted (or none seeded).
                // Fail with a clear domain error instead of an NRE in ConvertToOrder.
                throw new DeliveryOptionNotFoundException();
            }

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
                var payment = new Payment(DateTime.UtcNow, paymentRequest.PaymentToken, paymentRequest.PaymentMerchant, paymentRequest.Amount);

                order.SetPayment(payment);

                // The payment write (raw conditional UPDATE in OrderRepository) and the
                // order-status write (SaveChanges) run inside ONE transaction so a
                // failure between them cannot leave the payment columns committed while
                // the order stays Open — a paid-in-DB-but-never-submitted order with no
                // code path to recover. The confirmation email is sent only after the
                // commit, so a dispatch confirmation is never sent for a rolled-back order.
                var shouldSendConfirmationEmail = false;

                await using (var transaction = await _uow.BeginTransactionAsync())
                {
                    // Database-level double-payment guard: the in-memory lock only protects
                    // within a single process. If a concurrent IPN callback already recorded
                    // the payment, the conditional UPDATE affects zero rows and we refuse.
                    var paymentApplied = await _orderRepository.SetPaymentConditionallyAsync(order.Id, payment);

                    if (!paymentApplied)
                    {
                        throw new OrderAlreadyPaidForException("Order was already paid for by a concurrent request.");
                    }

                    shouldSendConfirmationEmail = MarkOrderSubmittedIfPaid(order);

                    _orderRepository.Save(order);
                    await _uow.CommitAsync();
                    await transaction.CommitAsync();
                }

                if (shouldSendConfirmationEmail)
                {
                    await SendOrderConfirmationEmailAsync(order, paymentRequest.CustomerEmail);
                }
            }
            catch (OrderAlreadyPaidForException)
            {
                _logger.LogError("Order {OrderId} was already paid for; refusing duplicate payment.", order.Id);

                throw;
            }
            catch (PaymentAmountDoesNotEqualOrderTotalException)
            {
                _logger.LogError("Payment amount for order {OrderId} does not match the order total; refusing invalid payment.", order.Id);

                throw;
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

        // Sets Status to Submitted when a payment is durably recorded and returns whether
        // the dispatch-confirmation email should be sent. Asserts the order has not been
        // submitted before (re-submitting after a partial failure is not recoverable).
        private bool MarkOrderSubmittedIfPaid(Order order)
        {
            if (order.Status != OrderStatus.Open)
            {
                throw new InvalidOperationException("You cannot submit this order as it has already been submitted.");
            }

            var orderHasBeenPaidFor = order.OrderHasBeenPaidFor();

            if (orderHasBeenPaidFor)
            {
                order.Status = OrderStatus.Submitted;
            }

            return orderHasBeenPaidFor;
        }

        // Sends the order confirmation email AFTER the payment + status write has been
        // committed (never inside the transaction), so a customer is not told their
        // order will be dispatched unless that state is durable.
        private async Task SendOrderConfirmationEmailAsync(Order order, string customerEmail)
        {
            var emailAddress = !string.IsNullOrWhiteSpace(customerEmail) ? customerEmail : order.Customer?.Email;

            if (string.IsNullOrWhiteSpace(emailAddress))
            {
                return;
            }

            var smtpPassword = _configuration["MailSettings:Smtp:Network:Password"] ?? _configuration["MailSettingsSmtpNetworkPassword"];

            if (string.IsNullOrWhiteSpace(smtpPassword))
            {
                return;
            }

            var emailBody = new StringBuilder();
            var emailSubject = string.Format("Order #{0}", order.Id);

            emailBody.AppendLine(string.Format("Hello {0},", order.Customer.FirstName));
            emailBody.AppendLine();
            emailBody.AppendLine("The following order will be packed and dispatched as soon as possible.");
            emailBody.AppendLine(order.ToString());
            emailBody.AppendLine();
            emailBody.AppendLine("Thank you for your custom.");

            var smtpUserName = _configuration["MailSettings:Smtp:Network:UserName"] ?? _configuration["MailSettingsSmtpNetworkUserName"];

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
}