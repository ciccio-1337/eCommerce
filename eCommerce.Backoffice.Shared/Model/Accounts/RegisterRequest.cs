using System.ComponentModel.DataAnnotations;

namespace eCommerce.Backoffice.Shared.Model.Accounts
{
    public class RegisterRequest
    {
        [Display(ShortName = "Identifier")]
        public string Id { get; set; }

        [Required]
        [StringLength(50)]
        [EmailAddress]
        [Display(ShortName = "E-Mail")]
        public string Email { get; set; }

        [Required]
        [StringLength(128, ErrorMessage = "{0} length must be between {2} and {1}.", MinimumLength = 8)]
        [Display(ShortName = "Hashed Password")]
        public string Password { get; set; }

        [Required]
        [Compare(nameof(Password), ErrorMessage = "Password mismatch")]
        [Display(ShortName = "Confirmed Hashed Password", Name="Confirm Password")]
        public string ConfirmPassword { get; set; }
    }
}