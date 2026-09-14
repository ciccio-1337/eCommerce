using System.Linq;
using System.Threading.Tasks;
using eCommerce.Storefront.Model.Orders;
using eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Implementations
{
    public class OrderRepository(IUnitOfWork uow, ShopDataContext dataContext) : Repository<Order, long>(uow, dataContext), IOrderRepository
    {
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