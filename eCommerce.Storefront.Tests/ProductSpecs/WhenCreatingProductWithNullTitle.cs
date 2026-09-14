using System.Linq;
using eCommerce.Storefront.Model.Products;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eCommerce.Storefront.Tests.ProductSpecs
{
    [TestClass]
    public class WhenCreatingProductWithNullTitle
    {
        private Product _product;

        [TestInitialize]
        public void Given()
        {
            _product = new Product()
            {
                Size = new ProductSize()
            };
        }

        [TestMethod]
        public void ThenItShouldHaveOneBrokenRule()
        {
            Assert.HasCount(1, _product.GetBrokenRules());
        }

        [TestMethod]
        public void ThenItShouldHaveBrokenRuleHighlightingTheRequirementForTitle()
        {
            Assert.AreEqual("Title", _product.GetBrokenRules().First().Property);
        }
    }
}