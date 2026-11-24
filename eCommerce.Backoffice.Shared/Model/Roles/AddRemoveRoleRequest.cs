namespace eCommerce.Backoffice.Shared.Model.Roles
{
    public class AddRemoveRoleRequest
    {
        public string RoleId { get; set; }
        public string UserId { get; set; }
        public bool Add { get; set; }

        // Step-up re-authentication for the acting administrator. Granting the
        // Admin role is a privilege-escalation primitive for a stolen session.
        public string CurrentPassword { get; set; }
    }
}