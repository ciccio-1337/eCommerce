using System.Threading.Tasks;

namespace eCommerce.Storefront.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendMailAsync(string from, string to, string subject, string body);
    }
}