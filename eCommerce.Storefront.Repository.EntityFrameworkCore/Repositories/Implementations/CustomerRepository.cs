using System;
using System.Linq;
using System.Threading.Tasks;
using eCommerce.Storefront.Model.Customers;
using eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Implementations
{
    public class CustomerRepository(IUnitOfWork uow, ShopDataContext dataContext) : Repository<Customer, long>(uow, dataContext), ICustomerRepository
    {
        public async Task<Customer> FindByAsync(string email)
        {
            var user = await _dataContext.Users.FirstOrDefaultAsync(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

            if (user != null)
            {
                var customer = await FindBy(c => c.UserId.Equals(user.Id)).FirstOrDefaultAsync();

                if (customer != null)
                {
                    customer.Email = user.Email;
                }

                return customer;
            }
            else
            {
                return null;
            }
        }

        public override async Task<Customer> FindByAsync(long id)
        {
            var customer = await base.FindByAsync(id);

            if (customer != null && !string.IsNullOrEmpty(customer.UserId))
            {
                var user = await _dataContext.Users.FirstOrDefaultAsync(u => u.Id.Equals(customer.UserId));

                if (user != null)
                {
                    customer.Email = user.Email;
                }
            }

            return customer;
        }

        public override IQueryable<Customer> AppendCriteria(IQueryable<Customer> criteria)
        {
            return criteria.Include(c => c.DeliveryAddressBook)
                           .Include(c => c.Orders)
                           .Include(c => c.Basket)
                           .ThenInclude(b => b.Items)
                           .ThenInclude(i => i.Product)
                           .ThenInclude(p => p.Title)
                           .Include(c => c.Basket)
                           .ThenInclude(b => b.DeliveryOption)
                           .ThenInclude(d => d.ShippingService);
        }

        public async Task SaveEmailAsync(string userId, string email)
        {
            var user = await _dataContext.Users.FirstOrDefaultAsync(u => u.Id.Equals(userId));

            if (user != null)
            {
                user.UserName = user.Email = email;
                user.NormalizedUserName = user.NormalizedEmail = email.ToUpper();

                _dataContext.Users.Update(user);
            }
        }
    }
}