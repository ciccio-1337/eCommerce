using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using eCommerce.Storefront.Model.Basket;
using eCommerce.Storefront.Model.Customers;
using eCommerce.Storefront.Model.Shipping;
using eCommerce.Storefront.Repository.EntityFrameworkCore;
using eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Interfaces;
using eCommerce.Storefront.Services.Implementations;
using eCommerce.Storefront.Services.Messaging.ProductCatalogService;
using eCommerce.Storefront.Services.ViewModels;
using MapsterMapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace eCommerce.Storefront.Tests.Services
{
    [TestClass]
    public class BasketServiceTests
    {
        private Mock<IBasketRepository> _basketRepositoryMock;
        private Mock<IProductRepository> _productRepositoryMock;
        private Mock<IDeliveryOptionRepository> _deliveryOptionRepositoryMock;
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<IMapper> _mapperMock;
        private Mock<ICustomerRepository> _customerRepositoryMock;
        private BasketService _basketService;

        [TestInitialize]
        public void Setup()
        {
            _basketRepositoryMock = new Mock<IBasketRepository>();
            _productRepositoryMock = new Mock<IProductRepository>();
            _deliveryOptionRepositoryMock = new Mock<IDeliveryOptionRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _mapperMock = new Mock<IMapper>();
            _customerRepositoryMock = new Mock<ICustomerRepository>();

            _basketService = new BasketService(
                _basketRepositoryMock.Object,
                _productRepositoryMock.Object,
                _deliveryOptionRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _mapperMock.Object,
                _customerRepositoryMock.Object);
        }

        [TestMethod]
        public async Task GetBasketAsync_WhenBasketNotFound_ReturnsEmptyBasketView()
        {
            var basketId = Guid.NewGuid();
            var request = new GetBasketRequest { BasketId = basketId };
            _basketRepositoryMock.Setup(x => x.FindByAsync(basketId)).ReturnsAsync((Basket)null);
            _mapperMock.Setup(x => x.Map<Basket, BasketView>(It.IsAny<Basket>())).Returns(new BasketView());

            var response = await _basketService.GetBasketAsync(request);

            Assert.IsNotNull(response);
            Assert.IsNotNull(response.Basket);
        }

        [TestMethod]
        public async Task CreateBasketAsync_WhenCustomerNotFound_ThrowsCustomerNotFoundException()
        {
            var request = new CreateBasketRequest { CustomerEmail = "missing@test.com", ProductsToAdd = new List<long>() };
            _customerRepositoryMock.Setup(x => x.FindByAsync("missing@test.com")).ReturnsAsync((Customer)null);

            await Assert.ThrowsExactlyAsync<CustomerNotFoundException>(() => _basketService.CreateBasketAsync(request));
        }

        [TestMethod]
        public void GetAllDispatchOptions_ReturnsMappedOptions()
        {
            var deliveryOptions = new List<DeliveryOption> { new DeliveryOption() }.AsQueryable();
            _deliveryOptionRepositoryMock.Setup(x => x.FindAll()).Returns(deliveryOptions);
            _mapperMock.Setup(x => x.Map<DeliveryOption, DeliveryOptionView>(It.IsAny<DeliveryOption>())).Returns(new DeliveryOptionView());

            var response = _basketService.GetAllDispatchOptions();

            Assert.IsNotNull(response);
            Assert.IsNotNull(response.DeliveryOptions);
        }
    }
}
