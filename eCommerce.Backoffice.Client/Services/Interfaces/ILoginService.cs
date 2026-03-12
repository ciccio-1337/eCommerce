using System.Threading.Tasks;

namespace eCommerce.Backoffice.Client.Services.Interfaces
{
    public interface ILoginService
    {
        Task LoginAsync(string token);
        Task LogoutAsync();
    }
}