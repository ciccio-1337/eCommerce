using System.Threading.Tasks;

namespace eCommerce.Storefront.Repository.EntityFrameworkCore
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ShopDataContext _dataContext;

        public UnitOfWork(ShopDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task CommitAsync()
        {
            await _dataContext.SaveChangesAsync();
        }
    }
}