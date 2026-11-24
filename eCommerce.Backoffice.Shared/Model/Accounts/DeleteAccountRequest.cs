namespace eCommerce.Backoffice.Shared.Model.Accounts
{
    /// <summary>
    /// Step-up credential for deleting an account. Sent in the request body, never
    /// in the URL, so the password does not end up in access logs or history.
    /// </summary>
    public class DeleteAccountRequest
    {
        public string CurrentPassword { get; set; }
    }
}
