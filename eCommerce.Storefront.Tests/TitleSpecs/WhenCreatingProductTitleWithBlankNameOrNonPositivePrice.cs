using System.Linq;
using eCommerce.Storefront.Model.Products;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eCommerce.Storefront.Tests.TitleSpecs
{
    [TestClass]
    public class WhenCreatingProductTitleWithBlankNameOrNonPositivePrice
    {
        private ProductTitle _title;

        [TestInitialize]
        public void Given()
        {
            _title = new ProductTitle()
            {
                Name = string.Empty,
                Price = 0m,
                Brand = new Brand(),
                Category = new Category(),
                Color = new ProductColor(),
                Products = null
            };
        }

        [TestMethod]
        public void ThenItShouldHaveTwoBrokenRules()
        {
            Assert.HasCount(2, _title.GetBrokenRules());
        }

        [TestMethod]
        public void ThenItShouldHaveBrokenRuleHighlightingTheRequirementForName()
        {
            Assert.IsTrue(_title.GetBrokenRules().Any(r => r.Property == "Name"));
        }

        [TestMethod]
        public void ThenItShouldHaveBrokenRuleHighlightingTheRequirementForPrice()
        {
            Assert.IsTrue(_title.GetBrokenRules().Any(r => r.Property == "Price"));
        }
    }
}