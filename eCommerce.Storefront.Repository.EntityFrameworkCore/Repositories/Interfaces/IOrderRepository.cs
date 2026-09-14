using System.Threading.Tasks;
using eCommerce.Storefront.Model.Orders;

namespace eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Interfaces
{
    public interface IOrderRepository : IRepository<Order, long>
    {
        /// <summary>
        /// Atomically sets the payment on an order only if it has not already been paid
        /// for at the database level. Returns true if the payment was applied, false if
        /// another concurrent request already set the payment.
        /// </summary>
        Task<bool> SetPaymentConditionallyAsync(long orderId, Payment payment);
    }
}