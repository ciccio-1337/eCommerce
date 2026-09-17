using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;

namespace eCommerce.Storefront.Repository.EntityFrameworkCore
{
    public interface IUnitOfWork
    {
        Task CommitAsync();
        // Starts an explicit database transaction so multiple repository writes
        // (including raw SQL through the same ShopDataContext) commit atomically.
        // The returned transaction must be committed by the caller; any exception
        // path rolls it back on disposal.
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}