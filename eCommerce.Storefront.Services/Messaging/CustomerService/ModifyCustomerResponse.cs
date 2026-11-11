using eCommerce.Storefront.Services.ViewModels;

namespace eCommerce.Storefront.Services.Messaging.CustomerService
{
    public class ModifyCustomerResponse
    {
        public CustomerView Customer { get; set; }

        /// <summary>
        /// True when the Identity email address actually changed, so the caller can
        /// send a confirmation link to the new address and force a re-login.
        /// </summary>
        public bool EmailChanged { get; set; }
    }
}