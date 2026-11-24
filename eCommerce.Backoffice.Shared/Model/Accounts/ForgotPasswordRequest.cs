using System.ComponentModel.DataAnnotations;

namespace eCommerce.Backoffice.Shared.Model.Accounts
{
    public class ForgotPasswordRequest
    {
        [Required]
        [StringLength(256)]
        [EmailAddress]
        public string Email { get; set; }
    }
}