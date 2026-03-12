using System.Collections.Generic;
using System.Threading.Tasks;

namespace eCommerce.Storefront.Controllers.Services.Interfaces
{
    public interface ICookieAuthentication
    {
        Task SetAuthenticationTokenAsync(string email, IEnumerable<string> roles);
        string GetAuthenticationToken();
        Task SignOutAsync();
    }
}