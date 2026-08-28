using System.Collections.Generic;
using eCommerce.Storefront.Services.ViewModels;
using eCommerce.Storefront.Services.Cache;
using eCommerce.Storefront.Services.Interfaces;
using eCommerce.Storefront.Controllers.Services.Interfaces;

namespace eCommerce.Storefront.Controllers.Controllers
{
    public abstract class ProductCatalogBaseController(ICookieAuthentication cookieAuthentication,
        ICustomerService customerService,
        ICachedProductCatalogService cachedProductCatalogService) : BaseController(cookieAuthentication,
            customerService)
    {
        protected readonly ICachedProductCatalogService _cachedProductCatalogService = cachedProductCatalogService;

        protected IEnumerable<CategoryView> GetCategories()
        {
            var response = _cachedProductCatalogService.GetAllCategories();

            return response.Categories;
        }
    }
}
