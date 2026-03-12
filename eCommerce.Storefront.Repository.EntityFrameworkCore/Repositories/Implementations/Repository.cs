using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using eCommerce.Storefront.Model;
using eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Implementations
{
    public class Repository<T, TId> : IRepository<T, TId> where T : EntityBase<TId>
    {
        protected readonly IUnitOfWork _uow;
        protected readonly ShopDataContext _dataContext;

        public Repository(IUnitOfWork uow, ShopDataContext dataContext)
        {
            _uow = uow;
            _dataContext = dataContext;
        }

        public async Task<T> FindByAsync(TId id)
        {
            return await AppendCriteria(_dataContext.Set<T>()).OrderBy(e => e.Id).FirstOrDefaultAsync(e => e.Id.Equals(id));
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
            _dataContext.Update(entity);
        }

        public void Remove(T entity)
        {
            _dataContext.Remove(entity);
        }
    }
}
