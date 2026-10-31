using System.Threading.Tasks;
using eCommerce.Backoffice.Shared.Model.Security;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.Backoffice.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AntiforgeryController(IAntiforgery antiforgery) : ControllerBase
    {
        [HttpGet("token")]
        [AllowAnonymous]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<ActionResult<AntiforgeryTokenResponse>> GetToken()
        {
            // The default authentication scheme is the storefront cookie, while the
            // backoffice uses the "Bearer" cookie scheme. Authenticate it explicitly
            // so a token requested after sign-in is bound to the administrator
            // identity; otherwise authenticated POSTs would reject it.
            var backofficeIdentity = await HttpContext.AuthenticateAsync(
                JwtBearerDefaults.AuthenticationScheme);

            if (backofficeIdentity.Succeeded && backofficeIdentity.Principal is not null)
            {
                HttpContext.User = backofficeIdentity.Principal;
            }

            var tokens = antiforgery.GetAndStoreTokens(HttpContext);

            return Ok(new AntiforgeryTokenResponse { Token = tokens.RequestToken });
        }
    }
}
