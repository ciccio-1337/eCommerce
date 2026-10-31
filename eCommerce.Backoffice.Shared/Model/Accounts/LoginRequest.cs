using System.ComponentModel.DataAnnotations;

namespace eCommerce.Backoffice.Shared.Model.Accounts
{
    public class LoginRequest
    {
        [Required]
        [StringLength(50)]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(128, ErrorMessage = "{0} length must be between {2} and {1}.", MinimumLength = 8)]
        public string Password { get; set; }
    }
}