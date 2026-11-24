using System.Collections.Generic;
using System.Threading.Tasks;
using eCommerce.Storefront.Controllers.Models;

namespace eCommerce.Storefront.Controllers.Services.Interfaces
{
    public interface ILocalAuthenticationService
    {
        Task<User> LoginAsync(string email, string password);
        Task<bool> ValidatePasswordAsync(string email, string password);
        Task<User> RegisterUserAsync(string email, string password, bool confirmEmail, IEnumerable<string> roles);
        Task<bool> ConfirmEmailAsync(string userId, string code);
        Task<string> GenerateEmailConfirmationTokenAsync(string userId);
        Task<string> GeneratePasswordResetTokenAsync(string userId);
        Task<bool> ResetPasswordAsync(string userId, string code, string password);
        Task DeleteUserAsync(string userId);
    }
}