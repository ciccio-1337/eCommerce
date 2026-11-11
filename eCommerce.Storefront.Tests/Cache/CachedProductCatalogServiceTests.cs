using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using eCommerce.Storefront.Model.Products;
using eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Interfaces;
using eCommerce.Storefront.Services.Cache;
using eCommerce.Storefront.Services.Interfaces;
using eCommerce.Storefront.Services.Messaging.ProductCatalogService;
using eCommerce.Storefront.Services.ViewModels;
using MapsterMapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace eCommerce.Storefront.Tests.Cache
{
    [TestClass]
    public class CachedProductCatalogServiceTests
    {
        private Mock<IProductCatalogService> _productCatalogServiceMock;
        private Mock<IProductTitleRepository> _productTitleRepositoryMock;
        private Mock<IMapper> _mapperMock;
        private FakeCacheStorage _cacheStorage;
        private CachedProductCatalogService _service;

        [TestInitialize]
        public void Setup()
        {
            _productCatalogServiceMock = new Mock<IProductCatalogService>();
            _productTitleRepositoryMock = new Mock<IProductTitleRepository>();
            _mapperMock = new Mock<IMapper>();
            _cacheStorage = new FakeCacheStorage();

            _service = new CachedProductCatalogService(
                _cacheStorage,
                _productCatalogServiceMock.Object,
                _productTitleRepositoryMock.Object,
                _mapperMock.Object);
        }

        [TestMethod]
        public void GetFeaturedProducts_ReturnsDeepCopiesAndCachesOnce()
        {
            _productCatalogServiceMock
                .Setup(x => x.GetFeaturedProducts())
                .Returns(new GetFeaturedProductsResponse
                {
                    Products = new List<ProductSummaryView>
                    {
                        new() { Id = 1, BrandName = "Brand", Name = "Original", Price = "€10" }
                    }
                });

            var first = _service.GetFeaturedProducts().Products.ToList();
            first[0].Name = "Mutated by caller";

            var second = _service.GetFeaturedProducts().Products.ToList();

            Assert.AreEqual("Original", second[0].Name);
            _productCatalogServiceMock.Verify(x => x.GetFeaturedProducts(), Times.Once);
        }

        [TestMethod]
        public void GetAllCategories_ReturnsDeepCopiesAndCachesOnce()
        {
            _productCatalogServiceMock
                .Setup(x => x.GetAllCategories())
                .Returns(new GetAllCategoriesResponse
                {
                    Categories = new List<CategoryView>
                    {
                        new() { Id = 1, Name = "Original" }
                    }
                });

            var first = _service.GetAllCategories().Categories.ToList();
            first[0].Name = "Mutated by caller";

            var second = _service.GetAllCategories().Categories.ToList();

            Assert.AreEqual("Original", second[0].Name);
            _productCatalogServiceMock.Verify(x => x.GetAllCategories(), Times.Once);
        }

        [TestMethod]
        public async Task GetProductAsync_CachesProductTitlesBetweenCalls()
        {
            var title = new ProductTitle { Id = 1, Name = "Product" };

            _productTitleRepositoryMock.Setup(x => x.FindAll()).Returns(new List<ProductTitle> { title }.AsQueryable());
            _mapperMock
                .Setup(x => x.Map<ProductTitle, ProductView>(title))
                .Returns(new ProductView { Id = 1, Name = "Product" });

            var first = await _service.GetProductAsync(new GetProductRequest { ProductId = 1 });
            var second = await _service.GetProductAsync(new GetProductRequest { ProductId = 1 });

            Assert.IsNotNull(first.Product);
            Assert.IsNotNull(second.Product);
            Assert.AreEqual("Product", second.Product.Name);
            _productTitleRepositoryMock.Verify(x => x.FindAll(), Times.Once);
        }

        private sealed class FakeCacheStorage : ICacheStorage
        {
            private readonly Dictionary<string, object> _items = [];

            public void Remove(string key) => _items.Remove(key);

            public void Store(string key, object data) => _items[key] = data;

            public T Retrieve<T>(string storageKey)
            {
                return _items.TryGetValue(storageKey, out var value) ? (T)value : default!;
            }

            public bool TryRetrieve<T>(string storageKey, out T value)
            {
                if (_items.TryGetValue(storageKey, out var stored))
                {
                    value = (T)stored;

                    return true;
                }

                value = default!;

                return false;
            }
        }
    }
}
