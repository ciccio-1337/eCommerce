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

        public async Task<T> FindByAsync(TId id)
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
            }

            foreach (var property in entry.Properties)
            {
                if (property.Metadata.IsKey())
                {
                    continue;
                }

                if (property.IsModified)
                {
                    continue;
                }

                property.IsModified = true;
            }
        }

        public void Remove(T entity)
        {
            _dataContext.Remove(entity);
        }
    }
}
