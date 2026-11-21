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
        // Detaches every tracked entity so a failed write (for example a unique-FK
        // race) can be retried from a clean change tracker instead of re-submitting
        // the entity that has just failed.
        void ClearChangeTracker();
    }
}