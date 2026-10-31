using System.Collections.Generic;

namespace eCommerce.Backoffice.Shared.Model.Accounts
{
    public class CurrentUserResponse
    {
        public string Email { get; set; }

        public IReadOnlyList<string> Roles { get; set; } = [];
    }
}
