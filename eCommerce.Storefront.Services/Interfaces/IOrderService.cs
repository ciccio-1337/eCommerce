using System.Threading.Tasks;
using eCommerce.Storefront.Services.Messaging.OrderService;

namespace eCommerce.Storefront.Services.Interfaces
{
    public interface IOrderService
    {
        Task<CreateOrderResponse> CreateOrderAsync(CreateOrderRequest request);
        Task<SetOrderPaymentResponse> SetOrderPaymentAsync(SetOrderPaymentRequest paymentRequest);
        Task<GetOrderResponse> GetOrderAsync(GetOrderRequest request);
    }
}