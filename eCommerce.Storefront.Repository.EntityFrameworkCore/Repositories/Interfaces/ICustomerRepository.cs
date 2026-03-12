using System.Threading.Tasks;
using eCommerce.Storefront.Model.Customers;

namespace eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Interfaces
{
    public interface ICustomerRepository : IRepository<Customer, long>
    {
        Task<Customer> FindByAsync(string email);
        Task SaveEmailAsync(string userId, string email);
    }
}