using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using eCommerce.Storefront.Model;

namespace eCommerce.Backoffice.Shared.Services.Interfaces
{
    public interface IEntityService<T, TId> where T : EntityBase<TId>
    {
        Task<T> GetAsync(TId id);
        IEnumerable<T> Get(Expression<Func<T, bool>> predicate);
        IEnumerable<T> Get(Expression<Func<T, bool>> predicate, int index, int count);
        IEnumerable<T> Get();
        Task<T> CreateAsync(T entity);
        Task<T> ModifyAsync(T entity);
        Task DeleteAsync(TId id);
    }
}