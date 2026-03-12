using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using eCommerce.Storefront.Model;

namespace eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Interfaces
{
    public interface IReadOnlyRepository<T, TId> where T : EntityBase<TId>
    {
        Task<T> FindByAsync(TId id);
        IQueryable<T> FindBy(Expression<Func<T, bool>> predicate);
        IQueryable<T> FindBy(Expression<Func<T, bool>> predicate, int index, int count);
        IQueryable<T> FindAll();
    }
}