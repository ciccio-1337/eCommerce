using System.Collections.Generic;

namespace eCommerce.Backoffice.Shared.Model.Accounts
{
    public class LoginResponse
    {
        public bool IsSuccess { get; set; }
        public IEnumerable<string> Errors { get; set; }
        public string Email { get; set; }
    }
}