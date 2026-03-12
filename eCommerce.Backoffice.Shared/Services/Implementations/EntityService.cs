using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using eCommerce.Backoffice.Shared.Services.Interfaces;
using eCommerce.Storefront.Model;
using eCommerce.Storefront.Repository.EntityFrameworkCore;
using eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Interfaces;

namespace eCommerce.Backoffice.Shared.Services.Implementations
{
    public class EntityService<T, TId> : IEntityService<T, TId> where T : EntityBase<TId>
    {
        private readonly IRepository<T, TId> _repository;
        private readonly IUnitOfWork _uow;

        public EntityService(IRepository<T, TId> repository, IUnitOfWork uow)
        {
            _repository = repository;
            _uow = uow;
        }

        public async Task<T> GetAsync(TId id)
        {
            return await _repository.FindByAsync(id);
        }

        public IEnumerable<T> Get(Expression<Func<T, bool>> predicate)
        {
            return _repository.FindBy(predicate);
        }

        public IEnumerable<T> Get(Expression<Func<T, bool>> predicate, int index, int count)
        {
            return _repository.FindBy(predicate, index, count);
        }

        public IEnumerable<T> Get()
        {
            return _repository.FindAll();
        }

        public async Task<T> CreateAsync(T entity)
        {
            entity.ThrowExceptionIfInvalid();
            await _repository.AddAsync(entity);
            await _uow.CommitAsync();

            return entity;
        }

        public async Task<T> ModifyAsync(T entity)
        {            
            entity.ThrowExceptionIfInvalid();
            _repository.Save(entity);
            await _uow.CommitAsync();

            return entity;
        }

        public async Task DeleteAsync(TId id)
        {
            T entity = await _repository.FindByAsync(id);

            _repository.Remove(entity);
            await _uow.CommitAsync();
        }
    }
}