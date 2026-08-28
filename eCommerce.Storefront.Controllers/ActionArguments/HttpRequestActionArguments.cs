using Microsoft.AspNetCore.Http;

namespace eCommerce.Storefront.Controllers.ActionArguments
{
    public class HttpRequestActionArguments(IHttpContextAccessor httpContextAccessor) : IActionArguments
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        public string GetValueForArgument(ActionArgumentKey key)
        {
            return _httpContextAccessor?.HttpContext?.Request?.Query?[key.ToString()];
        }
    }
}