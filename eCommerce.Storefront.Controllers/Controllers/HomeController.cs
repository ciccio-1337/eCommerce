using eCommerce.Storefront.Controllers.ViewModels.ProductCatalog;
using Microsoft.AspNetCore.Mvc;
using eCommerce.Storefront.Services.Cache;
using eCommerce.Storefront.Controllers.ViewModels;
using System.Diagnostics;
using eCommerce.Storefront.Services.Interfaces;
using eCommerce.Storefront.Controllers.Services.Interfaces;
using System.Threading.Tasks;

namespace eCommerce.Storefront.Controllers.Controllers
{
    public class HomeController : ProductCatalogBaseController
    {
        public HomeController(ICookieAuthentication cookieAuthentication,
            ICustomerService customerService,
            ICachedProductCatalogService cachedProductCatalogService) : base(cookieAuthentication,
                customerService,
                cachedProductCatalogService)
        {
        }

        public async Task<IActionResult> Index()
        {
            var homePageView = new HomePageView
            {
                Categories = GetCategories(),
                BasketSummary = await GetBasketSummaryViewAsync()
            };
            var response = _cachedProductCatalogService.GetFeaturedProducts();

            homePageView.Products = response.Products;
            
            return View(homePageView);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}