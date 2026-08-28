using System.Threading.Tasks;

namespace eCommerce.Storefront.Repository.EntityFrameworkCore
{
    public class UnitOfWork(ShopDataContext dataContext) : IUnitOfWork
    {
        private readonly ShopDataContext _dataContext = dataContext;

        public async Task CommitAsync()
        {
            await _dataContext.SaveChangesAsync();
        }
    }
}