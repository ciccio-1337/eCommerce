using System;

namespace eCommerce.Storefront.Controllers.Models
{
    /// <summary>
    /// Raised when registration targets an address that already has an account.
    /// The caller must respond exactly like a successful registration so the
    /// endpoint is not an account-enumeration oracle.
    /// </summary>
    public sealed class AccountAlreadyExistsException : Exception
    {
        public AccountAlreadyExistsException()
            : base("Registration could not be completed.")
        {
        }
    }
}
