using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;

namespace eCommerce.Storefront.Repository.EntityFrameworkCore
{
    public class UnitOfWork(ShopDataContext dataContext) : IUnitOfWork
    {
        private readonly ShopDataContext _dataContext = dataContext;

        public async Task CommitAsync()
        {
            await _dataContext.SaveChangesAsync();
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _dataContext.Database.BeginTransactionAsync();
        }
    }
}