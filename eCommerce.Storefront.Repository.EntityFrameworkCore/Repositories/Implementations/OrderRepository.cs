using System.Linq;
using System.Threading.Tasks;
using eCommerce.Storefront.Model.Orders;
using eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Implementations
{
    public class OrderRepository(IUnitOfWork uow, ShopDataContext dataContext) : Repository<Order, long>(uow, dataContext), IOrderRepository
    {
        public async Task<bool> SetPaymentConditionallyAsync(long orderId, Payment payment)
        {
            // Use a conditional UPDATE so that two concurrent IPN callbacks cannot
            // both succeed: the second UPDATE finds PaymentTransactionId already set
            // and affects zero rows. The Payment owned type is stored as plain columns
            // on the Orders table (see OrderConfiguration), so a parameterized UPDATE
            // is safe on both SQLite and PostgreSQL.
            var affected = await _dataContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                UPDATE "Orders"
                SET "PaymentDate" = {payment.DatePaid},
                    "PaymentTransactionId" = {payment.TransactionId},
                    "PaymentMerchant" = {payment.Merchant},
                    "PaymentAmount" = {payment.Amount}
                WHERE "OrderId" = {orderId} AND "PaymentTransactionId" IS NULL
                """);

            return affected > 0;
        }

        public override async Task<Order> FindByAsync(long id)
        {
            var order = await base.FindByAsync(id);

            if (order?.Customer != null && !string.IsNullOrEmpty(order.Customer.UserId))
            {
                var user = await _dataContext.Users.FirstOrDefaultAsync(u => u.Id.Equals(order.Customer.UserId));

                if (user != null)
                {
                    order.Customer.Email = user.Email;
                }
            }

            return order;
        }

        public override IQueryable<Order> AppendCriteria(IQueryable<Order> criteria)
        {
            return criteria.Include(o => o.Items)
                           .ThenInclude(i => i.Product)
                           .ThenInclude(p => p.Size)
                           .Include(o => o.Items)
                           .ThenInclude(i => i.Product)
                           .ThenInclude(p => p.Title)
                           .Include(o => o.ShippingService)
                           .ThenInclude(s => s.Courier)
                           .Include(o => o.DeliveryAddress)
                           .Include(o => o.Customer)
                           .ThenInclude(c => c.DeliveryAddressBook);
        }
    }
}