using eCommerce.Storefront.Model.Products;
using eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Interfaces;
using eCommerce.Storefront.Services.Implementations;
using eCommerce.Storefront.Services.Messaging.ProductCatalogService;
using MapsterMapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace eCommerce.Storefront.Tests.Services
{
    [TestClass]
    public class ProductCatalogServiceTests
    {
        [TestMethod]
        public void CreateProductSearchResultFrom_WhenNoProductsMatch_ReportsZeroPages()
        {
            var service = new ProductCatalogService(
                Mock.Of<IProductTitleRepository>(),
                Mock.Of<IProductRepository>(),
                Mock.Of<IReadOnlyRepository<Category, long>>(),
                Mock.Of<IMapper>());

            var response = service.CreateProductSearchResultFrom(
                [],
                new GetProductsByCategoryRequest
                {
                    Index = 1,
                    NumberOfResultsPerPage = 6
                });

            Assert.AreEqual(0, response.NumberOfTitlesFound);
            // JSON clients consume this value directly; "1 page" made an empty
            // result look like a single empty page.
            Assert.AreEqual(0, response.TotalNumberOfPages);
        }
    }
}
