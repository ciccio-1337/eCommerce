using System.Linq;
using eCommerce.Storefront.Model.Shipping;
using eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Implementations
{
    public class DeliveryOptionRepository(IUnitOfWork uow, ShopDataContext dataContext) : Repository<DeliveryOption, long>(uow, dataContext), IDeliveryOptionRepository
    {
        public override IQueryable<DeliveryOption> AppendCriteria(IQueryable<DeliveryOption> criteria)
        {
            return criteria.Include(d => d.ShippingService);
        }
    }
}