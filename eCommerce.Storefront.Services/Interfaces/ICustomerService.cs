using System.Threading.Tasks;
using eCommerce.Storefront.Services.Messaging.CustomerService;

namespace eCommerce.Storefront.Services.Interfaces
{
    public interface ICustomerService
    {
        Task<CreateCustomerResponse> CreateCustomerAsync(CreateCustomerRequest request);
        Task<GetCustomerResponse> GetCustomerAsync(GetCustomerRequest request);
        Task<ModifyCustomerResponse> ModifyCustomerAsync(ModifyCustomerRequest request);
        Task<DeliveryAddressModifyResponse> ModifyDeliveryAddressAsync(DeliveryAddressModifyRequest request);
        Task<DeliveryAddressAddResponse> AddDeliveryAddressAsync(DeliveryAddressAddRequest request);
    }
}