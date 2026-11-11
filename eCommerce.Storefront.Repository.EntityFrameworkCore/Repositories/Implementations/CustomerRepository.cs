using System;
using System.Linq;
using System.Threading.Tasks;
using eCommerce.Storefront.Model.Customers;
using eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Implementations
{
    public class CustomerRepository(IUnitOfWork uow,
        ShopDataContext dataContext,
        ILookupNormalizer lookupNormalizer) : Repository<Customer, long>(uow, dataContext), ICustomerRepository
    {
        private readonly ILookupNormalizer _lookupNormalizer = lookupNormalizer;
        public async Task<Customer> FindByAsync(string email)
        {
            // Identity stores NormalizedEmail via the configured ILookupNormalizer;
            // matching against the same normalization avoids both EF translation issues
            // with StringComparison and case-sensitivity problems at the SQL level.
            var normalisedEmail = _lookupNormalizer.NormalizeEmail(email) ?? string.Empty;
            var user = await _dataContext.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalisedEmail);

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

            if (user == null)
            {
                return;
            }

            var normalizedEmail = _lookupNormalizer.NormalizeEmail(email) ?? string.Empty;

            if (string.Equals(user.NormalizedEmail, normalizedEmail, StringComparison.Ordinal))
            {
                return;
            }

            user.UserName = user.Email = email;
            user.NormalizedEmail = normalizedEmail;
            user.NormalizedUserName = _lookupNormalizer.NormalizeName(email) ?? normalizedEmail;

            // Changing the sign-in address is a security-sensitive operation: the new
            // address has not been proven yet, and existing sessions must be revoked.
            user.EmailConfirmed = false;
            user.SecurityStamp = Guid.NewGuid().ToString();

            _dataContext.Users.Update(user);
        }
    }
}