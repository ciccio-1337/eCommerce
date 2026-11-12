using System.Linq;
using System.Threading.Tasks;
using eCommerce.Storefront.Controllers.Controllers;
using eCommerce.Storefront.Controllers.DTOs;
using eCommerce.Storefront.Controllers.Services.Interfaces;
using eCommerce.Storefront.Services.Cache;
using eCommerce.Storefront.Services.Interfaces;
using eCommerce.Storefront.Services.Messaging.ProductCatalogService;
using eCommerce.Storefront.Services.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace eCommerce.Storefront.Tests.Controllers
{
    [TestClass]
    public class BasketControllerBoundsTests
    {
        private const int MaxQuantityPerLine = 1000;
        private const int MaxLinesPerUpdate = 100;

        private Mock<IBasketService> _basketServiceMock;
        private BasketController _controller;

        [TestInitialize]
        public void Setup()
        {
            _basketServiceMock = new Mock<IBasketService>();
            _controller = new BasketController(
                Mock.Of<ICachedProductCatalogService>(),
                _basketServiceMock.Object,
                Mock.Of<ICookieAuthentication>(),
                Mock.Of<ICustomerService>());
        }

        [TestMethod]
        public async Task UpdateItems_WithQuantityAboveMaximum_ReturnsBadRequest()
        {
            var request = new BasketQtyUpdateRequest
            {
                Items = [new BasketItemUpdateRequest { ProductId = 1, Qty = MaxQuantityPerLine + 1 }]
            };

            var result = await _controller.UpdateItems(request);

            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            _basketServiceMock.Verify(
                x => x.ModifyBasketAsync(It.IsAny<ModifyBasketRequest>()),
                Times.Never);
        }

        [TestMethod]
        public async Task UpdateItems_WithTooManyLines_ReturnsBadRequest()
        {
            var request = new BasketQtyUpdateRequest
            {
                Items = Enumerable.Range(1, MaxLinesPerUpdate + 1)
                    .Select(id => new BasketItemUpdateRequest { ProductId = id, Qty = 1 })
                    .ToArray()
            };

            var result = await _controller.UpdateItems(request);

            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            _basketServiceMock.Verify(
                x => x.ModifyBasketAsync(It.IsAny<ModifyBasketRequest>()),
                Times.Never);
        }

        [TestMethod]
        public async Task UpdateItems_WithQuantityAtMaximum_IsAccepted()
        {
            _basketServiceMock
                .Setup(x => x.ModifyBasketAsync(It.IsAny<ModifyBasketRequest>()))
                .ReturnsAsync(new ModifyBasketResponse
                {
                    Basket = new BasketView { BasketTotal = "10.00", NumberOfItems = 1 }
                });
            _basketServiceMock
                .Setup(x => x.GetAllDispatchOptions())
                .Returns(new GetAllDispatchOptionsResponse { DeliveryOptions = [] });

            var request = new BasketQtyUpdateRequest
            {
                Items = [new BasketItemUpdateRequest { ProductId = 1, Qty = MaxQuantityPerLine }]
            };

            var result = await _controller.UpdateItems(request);

            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            _basketServiceMock.Verify(
                x => x.ModifyBasketAsync(It.Is<ModifyBasketRequest>(
                    r => r.ItemsToUpdate.Single().NewQty == MaxQuantityPerLine)),
                Times.Once);
        }
    }
}
