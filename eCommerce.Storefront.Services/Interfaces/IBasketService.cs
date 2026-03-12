using System.Threading.Tasks;
using eCommerce.Storefront.Services.Messaging.ProductCatalogService;

namespace eCommerce.Storefront.Services.Interfaces
{
    public interface IBasketService
    {
        Task<GetBasketResponse> GetBasketAsync(GetBasketRequest basketRequest);
        Task<CreateBasketResponse> CreateBasketAsync(CreateBasketRequest basketRequest);
        Task<ModifyBasketResponse> ModifyBasketAsync(ModifyBasketRequest request);
        GetAllDispatchOptionsResponse GetAllDispatchOptions();
    }
}