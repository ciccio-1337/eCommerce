namespace eCommerce.Backoffice.Shared.Model.Accounts
{
    public class EmailConfirmationRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string UserId { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public string Code { get; set; }
    }
}