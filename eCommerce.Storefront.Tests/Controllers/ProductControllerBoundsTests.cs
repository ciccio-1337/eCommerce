using System.Collections.Generic;
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
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace eCommerce.Storefront.Tests.Controllers
{
    [TestClass]
    public class ProductControllerBoundsTests
    {
        private const int MaxPageIndex = 10_000;
        private const int MaxRefinementsPerGroup = 50;

        private Mock<ICachedProductCatalogService> _catalogServiceMock;
        private ProductController _controller;
        private GetProductsByCategoryRequest _capturedRequest;

        [TestInitialize]
        public void Setup()
        {
            var configurationMock = new Mock<IConfiguration>();
            configurationMock.Setup(x => x["NumberOfResultsPerPage"]).Returns("6");

            _catalogServiceMock = new Mock<ICachedProductCatalogService>();
            _catalogServiceMock
                .Setup(x => x.GetAllCategories())
                .Returns(new GetAllCategoriesResponse { Categories = [] });
            _catalogServiceMock
                .Setup(x => x.GetProductsByCategoryAsync(It.IsAny<GetProductsByCategoryRequest>()))
                .Callback<GetProductsByCategoryRequest>(request => _capturedRequest = request)
                .ReturnsAsync(new GetProductsByCategoryResponse
                {
                    Products = [],
                    RefinementGroups = []
                });

            _controller = new ProductController(
                configurationMock.Object,
                Mock.Of<ICookieAuthentication>(),
                Mock.Of<ICustomerService>(),
                _catalogServiceMock.Object);
        }

        [TestMethod]
        public async Task GetProducts_WithNegativeIndex_ClampsToOne()
        {
            await _controller.GetProducts(new ProductSearchRequest { Index = -5 });

            Assert.AreEqual(1, _capturedRequest.Index);
        }

        [TestMethod]
        public async Task GetProducts_WithOverflowingIndex_ClampsToMaximum()
        {
            await _controller.GetProducts(new ProductSearchRequest { Index = int.MaxValue });

            Assert.AreEqual(MaxPageIndex, _capturedRequest.Index);
        }

        [TestMethod]
        public async Task GetProducts_WithTooManyRefinements_ReturnsBadRequest()
        {
            var request = new ProductSearchRequest
            {
                ColorIds = Enumerable.Range(0, MaxRefinementsPerGroup + 1).Select(id => (long)id).ToArray()
            };

            var result = await _controller.GetProducts(request);

            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            _catalogServiceMock.Verify(
                x => x.GetProductsByCategoryAsync(It.IsAny<GetProductsByCategoryRequest>()),
                Times.Never);
        }

        [TestMethod]
        public async Task GetProducts_WithRefinementsAtTheLimit_IsAccepted()
        {
            var request = new ProductSearchRequest
            {
                RefinementGroups =
                [
                    new eCommerce.Storefront.Controllers.DTOs.RefinementGroup
                    {
                        GroupId = (int)RefinementGroupings.Color,
                        SelectedRefinements = Enumerable.Range(0, MaxRefinementsPerGroup)
                            .Select(id => (long)id)
                            .ToArray()
                    }
                ]
            };

            var result = await _controller.GetProducts(request);

            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            Assert.AreEqual(MaxRefinementsPerGroup, _capturedRequest.ColorIds.Length);
        }

        [TestMethod]
        public async Task GetProducts_WithRefinementsInGroups_LimitsEachGroup()
        {
            var request = new ProductSearchRequest
            {
                RefinementGroups =
                [
                    new eCommerce.Storefront.Controllers.DTOs.RefinementGroup
                    {
                        GroupId = (int)RefinementGroupings.Color,
                        SelectedRefinements = Enumerable.Range(0, MaxRefinementsPerGroup + 10)
                            .Select(id => (long)id)
                            .ToArray()
                    }
                ]
            };

            await _controller.GetProducts(request);

            Assert.AreEqual(MaxRefinementsPerGroup, _capturedRequest.ColorIds.Length);
        }
    }
}
