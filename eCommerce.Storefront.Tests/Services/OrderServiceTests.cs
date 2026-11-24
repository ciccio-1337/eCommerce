using System;
using System.Threading.Tasks;
using eCommerce.Storefront.Model.Basket;
using eCommerce.Storefront.Model.Customers;
using eCommerce.Storefront.Model.Orders;
using eCommerce.Storefront.Repository.EntityFrameworkCore;
using eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Interfaces;
using eCommerce.Storefront.Services.Implementations;
using eCommerce.Storefront.Services.Interfaces;
using eCommerce.Storefront.Services.Messaging.OrderService;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace eCommerce.Storefront.Tests.Services
{
    [TestClass]
    public class OrderServiceTests
    {
        private Mock<IOrderRepository> _orderRepositoryMock;
        private Mock<IBasketRepository> _basketRepositoryMock;
        private Mock<ICustomerRepository> _customerRepositoryMock;
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<IMapper> _mapperMock;
        private Mock<ILogger<OrderService>> _loggerMock;
        private Mock<IConfiguration> _configurationMock;
        private Mock<IEmailService> _emailServiceMock;
        private OrderService _orderService;

        [TestInitialize]
        public void Setup()
        {
            _orderRepositoryMock = new Mock<IOrderRepository>();
            _basketRepositoryMock = new Mock<IBasketRepository>();
            _customerRepositoryMock = new Mock<ICustomerRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<OrderService>>();
            _configurationMock = new Mock<IConfiguration>();
            _emailServiceMock = new Mock<IEmailService>();

            _orderService = new OrderService(
                _orderRepositoryMock.Object,
                _basketRepositoryMock.Object,
                _customerRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _loggerMock.Object,
                _configurationMock.Object,
                _emailServiceMock.Object);
        }

        [TestMethod]
        public async Task CreateOrderAsync_WhenCustomerNotFound_ThrowsCustomerNotFoundException()
        {
            var request = new CreateOrderRequest { CustomerEmail = "test@test.com", BasketId = Guid.NewGuid(), DeliveryId = 1 };
            _customerRepositoryMock.Setup(x => x.FindByAsync("test@test.com")).ReturnsAsync((Customer)null);

            await Assert.ThrowsExactlyAsync<CustomerNotFoundException>(() => _orderService.CreateOrderAsync(request));
        }

        [TestMethod]
        public async Task CreateOrderAsync_WhenBasketNotFound_ThrowsBasketDoesNotExistException()
        {
            var request = new CreateOrderRequest { CustomerEmail = "test@test.com", BasketId = Guid.NewGuid(), DeliveryId = 1 };
            var customer = new Customer { Email = "test@test.com", FirstName = "Test", SecondName = "Test", UserId = "123" };
            _customerRepositoryMock.Setup(x => x.FindByAsync("test@test.com")).ReturnsAsync(customer);
            _basketRepositoryMock.Setup(x => x.FindByAsync(request.BasketId)).ReturnsAsync((Basket)null);

            await Assert.ThrowsExactlyAsync<BasketDoesNotExistException>(() => _orderService.CreateOrderAsync(request));
        }

        [TestMethod]
        public async Task CreateOrderAsync_WhenBasketBelongsToAnotherCustomer_ThrowsBasketDoesNotExistException()
        {
            var request = new CreateOrderRequest { CustomerEmail = "test@test.com", BasketId = Guid.NewGuid(), DeliveryId = 1 };
            var customer = new Customer { Id = 1, Email = "test@test.com", FirstName = "Test", SecondName = "Test", UserId = "123" };
            var basket = new Basket { Id = request.BasketId };
            basket.SetCustomer(new Customer { Id = 2, Email = "other@test.com", FirstName = "Other", SecondName = "Other", UserId = "456" });
            _customerRepositoryMock.Setup(x => x.FindByAsync("test@test.com")).ReturnsAsync(customer);
            _basketRepositoryMock.Setup(x => x.FindByAsync(request.BasketId)).ReturnsAsync(basket);

            await Assert.ThrowsExactlyAsync<BasketDoesNotExistException>(() => _orderService.CreateOrderAsync(request));
            _orderRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Order>()), Times.Never);
        }

        [TestMethod]
        public async Task SetOrderPaymentAsync_WhenCustomerDoesNotOwnOrder_ThrowsOrderNotFoundException()
        {
            var request = new SetOrderPaymentRequest
            {
                OrderId = 5,
                CustomerEmail = "attacker@test.com",
                PaymentToken = "token",
                Amount = 10m
            };
            var order = new Order { Id = 5, Customer = new Customer { Id = 1, Email = "owner@test.com", FirstName = "Owner", SecondName = "Owner", UserId = "1" } };
            _orderRepositoryMock.Setup(x => x.FindByAsync(5)).ReturnsAsync(order);
            _customerRepositoryMock
                .Setup(x => x.FindByAsync("attacker@test.com"))
                .ReturnsAsync(new Customer { Id = 2, Email = "attacker@test.com", FirstName = "A", SecondName = "B", UserId = "2" });

            await Assert.ThrowsExactlyAsync<OrderNotFoundException>(() => _orderService.SetOrderPaymentAsync(request));
            _orderRepositoryMock.Verify(
                x => x.SetPaymentConditionallyAsync(It.IsAny<long>(), It.IsAny<Payment>()),
                Times.Never);
        }

        [TestMethod]
        public async Task GetOrderAsync_WhenOrderNotFound_ReturnsEmptyResponse()
        {
            var request = new GetOrderRequest { OrderId = 1 };
            _orderRepositoryMock.Setup(x => x.FindByAsync(1)).ReturnsAsync((Order)null);

            var response = await _orderService.GetOrderAsync(request);

            Assert.IsNotNull(response);
            Assert.IsNull(response.Order);
        }
    }
}
