using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using eCommerce.Storefront.Model.Basket;
using eCommerce.Storefront.Model.Customers;
using eCommerce.Storefront.Model.Products;
using eCommerce.Storefront.Model.Shipping;
using eCommerce.Storefront.Repository.EntityFrameworkCore;
using eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Interfaces;
using eCommerce.Storefront.Services.Implementations;
using eCommerce.Storefront.Services.Messaging.ProductCatalogService;
using eCommerce.Storefront.Services.ViewModels;
using eCommerce.Storefront.Tests.Infrastructure;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
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
        public async Task CreateBasketAsync_WhenNoDeliveryOptionExists_ThrowsDeliveryOptionNotFoundException()
        {
            var request = new CreateBasketRequest
            {
                CustomerEmail = "customer@test.com",
                ProductsToAdd = new List<long>()
            };

            _customerRepositoryMock
                .Setup(x => x.FindByAsync("customer@test.com"))
                .ReturnsAsync(new Customer
                {
                    UserId = "user-1",
                    FirstName = "John",
                    SecondName = "Doe",
                    Email = "customer@test.com"
                });
            _deliveryOptionRepositoryMock
                .Setup(x => x.FindAll())
                .Returns(new List<DeliveryOption>().AsAsyncQueryable());

            await Assert.ThrowsExactlyAsync<DeliveryOptionNotFoundException>(() => _basketService.CreateBasketAsync(request));
        }

        [TestMethod]
        public async Task CreateBasketAsync_WhenCustomerNotFound_ThrowsCustomerNotFoundException()
        {
            var request = new CreateBasketRequest { CustomerEmail = "missing@test.com", ProductsToAdd = new List<long>() };
            _customerRepositoryMock.Setup(x => x.FindByAsync("missing@test.com")).ReturnsAsync((Customer)null);

            await Assert.ThrowsExactlyAsync<CustomerNotFoundException>(() => _basketService.CreateBasketAsync(request));
        }

        [TestMethod]
        public async Task ModifyBasketAsync_WhenLastItemRemoved_PersistsTheEmptyBasket()
        {
            var customer = CreateCustomer();
            var basket = CreateBasket(customer);
            var product = CreateProduct(1);
            basket.Add(product);

            _basketRepositoryMock.Setup(x => x.FindByAsync(basket.Id)).ReturnsAsync(basket);
            _productRepositoryMock
                .Setup(x => x.FindBy(It.IsAny<Expression<Func<Product, bool>>>()))
                .Returns(new List<Product> { product }.AsAsyncQueryable());
            _mapperMock
                .Setup(x => x.Map<Basket, BasketView>(It.IsAny<Basket>()))
                .Returns(new BasketView());

            var response = await _basketService.ModifyBasketAsync(new ModifyBasketRequest
            {
                BasketId = basket.Id,
                ItemsToRemove = [product.Id]
            });

            Assert.IsNotNull(response.Basket);
            Assert.AreEqual(0, basket.Items.Count);
            // The deletion must actually be flushed; the old early return skipped
            // both calls and the last line reappeared on reload.
            _basketRepositoryMock.Verify(x => x.Save(basket), Times.Once);
            _unitOfWorkMock.Verify(x => x.CommitAsync(), Times.Once);
        }

        [TestMethod]
        public async Task CreateBasketAsync_WhenRequestedProductDoesNotExist_ThrowsProductNotFound()
        {
            _customerRepositoryMock
                .Setup(x => x.FindByAsync("customer@test.com"))
                .ReturnsAsync(CreateCustomer());
            _deliveryOptionRepositoryMock
                .Setup(x => x.FindAll())
                .Returns(new List<DeliveryOption> { new() }.AsAsyncQueryable());
            _productRepositoryMock
                .Setup(x => x.FindBy(It.IsAny<Expression<Func<Product, bool>>>()))
                .Returns(new List<Product>().AsAsyncQueryable());

            var request = new CreateBasketRequest { CustomerEmail = "customer@test.com" };
            request.ProductsToAdd.Add(999);

            await Assert.ThrowsExactlyAsync<ProductNotFoundException>(() => _basketService.CreateBasketAsync(request));
            _customerRepositoryMock.Verify(x => x.Save(It.IsAny<Customer>()), Times.Never);
        }

        [TestMethod]
        public async Task ModifyBasketAsync_WhenRequestedProductDoesNotExist_ThrowsProductNotFound()
        {
            var basket = CreateBasket(CreateCustomer());
            _basketRepositoryMock.Setup(x => x.FindByAsync(basket.Id)).ReturnsAsync(basket);
            _productRepositoryMock
                .Setup(x => x.FindBy(It.IsAny<Expression<Func<Product, bool>>>()))
                .Returns(new List<Product>().AsAsyncQueryable());

            var request = new ModifyBasketRequest { BasketId = basket.Id };
            request.ProductsToAdd.Add(999);

            await Assert.ThrowsExactlyAsync<ProductNotFoundException>(() => _basketService.ModifyBasketAsync(request));
            _unitOfWorkMock.Verify(x => x.CommitAsync(), Times.Never);
        }

        [TestMethod]
        public async Task CreateBasketAsync_WhenConcurrentInsertLoses_ClearsTrackerAndAddsToWinnersBasket()
        {
            var email = "customer@test.com";
            var losingCustomer = CreateCustomer(email);
            var winningCustomer = CreateCustomer(email);
            var winningBasket = CreateBasket(winningCustomer);
            winningCustomer.AddBasket(winningBasket);
            var product = CreateProduct(1);

            _customerRepositoryMock
                .SetupSequence(x => x.FindByAsync(email))
                .ReturnsAsync(losingCustomer)
                .ReturnsAsync(winningCustomer);
            _deliveryOptionRepositoryMock
                .Setup(x => x.FindAll())
                .Returns(new List<DeliveryOption> { new() }.AsAsyncQueryable());
            _productRepositoryMock
                .Setup(x => x.FindBy(It.IsAny<Expression<Func<Product, bool>>>()))
                .Returns(new List<Product> { product }.AsAsyncQueryable());
            _unitOfWorkMock
                .SetupSequence(x => x.CommitAsync())
                .ThrowsAsync(new DbUpdateException())
                .Returns(Task.CompletedTask);
            _mapperMock
                .Setup(x => x.Map<Basket, BasketView>(It.IsAny<Basket>()))
                .Returns(new BasketView());

            var request = new CreateBasketRequest { CustomerEmail = email };
            request.ProductsToAdd.Add(product.Id);

            var response = await _basketService.CreateBasketAsync(request);

            Assert.IsNotNull(response.Basket);
            // The failed graph must be detached before the retry, otherwise the same
            // INSERT is replayed and the recovery path throws again.
            _unitOfWorkMock.Verify(x => x.ClearChangeTracker(), Times.Once);
            Assert.AreEqual(1, winningBasket.Items.Count);
            _customerRepositoryMock.Verify(x => x.Save(It.IsAny<Customer>()), Times.Exactly(2));
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

        private static Customer CreateCustomer(string email = "customer@test.com") => new()
        {
            UserId = "user-1",
            FirstName = "John",
            SecondName = "Doe",
            Email = email
        };

        private static Basket CreateBasket(Customer customer)
        {
            var basket = new Basket();
            basket.SetCustomer(customer);
            basket.SetDeliveryOption(new DeliveryOption());

            return basket;
        }

        private static Product CreateProduct(long id) => new()
        {
            Id = id,
            Title = new ProductTitle { Id = id, Name = "Product", Price = 10m }
        };
    }
}
