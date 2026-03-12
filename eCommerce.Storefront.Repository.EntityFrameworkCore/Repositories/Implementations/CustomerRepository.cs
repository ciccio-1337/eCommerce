using System.Linq;
using System.Threading.Tasks;
using eCommerce.Storefront.Model.Customers;
using eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Implementations
{
    public class CustomerRepository : Repository<Customer, long>, ICustomerRepository
    {
        public CustomerRepository(IUnitOfWork uow, ShopDataContext dataContext) : base(uow, dataContext)
        {
        }

        public async Task<Customer> FindByAsync(string email)
        {
            var user = await _dataContext.Users.FirstOrDefaultAsync(u => u.Email.Equals(email));

            if (user != null)
            {
                return await FindBy(c => c.UserId.Equals(user.Id)).FirstOrDefaultAsync();
            }
            else 
            {
                return null;
            }
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
                           .ThenInclude(b => b.DeliveryOption);
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