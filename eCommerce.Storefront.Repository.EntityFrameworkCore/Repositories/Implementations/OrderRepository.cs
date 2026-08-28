using System.Linq;
using eCommerce.Storefront.Model.Orders;
using eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Implementations
{
    public class OrderRepository(IUnitOfWork uow, ShopDataContext dataContext) : Repository<Order, long>(uow, dataContext), IOrderRepository
    {
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