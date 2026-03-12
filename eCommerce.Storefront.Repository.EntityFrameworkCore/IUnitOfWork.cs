using System.Threading.Tasks;

namespace eCommerce.Storefront.Repository.EntityFrameworkCore
{
    public interface IUnitOfWork
    {
        Task CommitAsync();
    }
}