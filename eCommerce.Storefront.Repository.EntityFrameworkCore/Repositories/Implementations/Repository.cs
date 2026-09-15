using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using eCommerce.Storefront.Model;
using eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Implementations
{
    public class Repository<T, TId>(IUnitOfWork uow, ShopDataContext dataContext) : IRepository<T, TId> where T : EntityBase<TId>
    {
        protected readonly IUnitOfWork _uow = uow;
        protected readonly ShopDataContext _dataContext = dataContext;

        public virtual async Task<T> FindByAsync(TId id)
        {
            return await AppendCriteria(_dataContext.Set<T>()).FirstOrDefaultAsync(e => e.Id.Equals(id));
        }

        public IQueryable<T> FindBy(Expression<Func<T, bool>> predicate)
        {
            return AppendCriteria(_dataContext.Set<T>()).Where(predicate);
        }

        public IQueryable<T> FindBy(Expression<Func<T, bool>> predicate, int index, int count)
        {
            return AppendCriteria(_dataContext.Set<T>()).Where(predicate).OrderBy(e => e.Id).Skip(index).Take(count);
        }

        public IQueryable<T> FindAll()
        {
            return AppendCriteria(_dataContext.Set<T>());
        }

        public virtual IQueryable<T> AppendCriteria(IQueryable<T> criteria)
        {
            return criteria;
        }

        public async Task AddAsync(T entity)
        {
            await _dataContext.AddAsync(entity);
        }

        public void Save(T entity)
        {
            var entry = _dataContext.Entry(entity);

            if (entry.State == EntityState.Detached)
            {
                if (Equals(entity.Id, default(TId)))
                {
                    _dataContext.Set<T>().Add(entity);

                    return;
                }

                var attached = _dataContext.Set<T>().Local.FirstOrDefault(e => Equals(e.Id, entity.Id));

                if (attached != null)
                {
                    _dataContext.Entry(attached).CurrentValues.SetValues(entity);

                    return;
                }

                _dataContext.Set<T>().Attach(entity);

                // The entity arrived here detached (e.g. rebuilt from a JSON DTO by the
                // backoffice). Attach alone leaves it Unchanged, so SaveChanges would emit
                // no UPDATE and silently drop the edit. Marking the whole row Modified is
                // the correct PUT semantics for this path.
                _dataContext.Entry(entity).State = EntityState.Modified;
            }

            // Tracked entities (loaded through this context by storefront services) skip
            // the branch above: EF Core's change tracker diffs them against their original
            // values, so no manual IsModified forcing is needed (forced marking would
            // clobber concurrent changes to unrelated fields).
        }

        public void Remove(T entity)
        {
            _dataContext.Remove(entity);
        }
    }
}
