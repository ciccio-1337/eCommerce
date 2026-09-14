using System;

namespace eCommerce.Storefront.Services.Implementations
{
    public class BasketAlreadyExistsException : Exception
    {
        public BasketAlreadyExistsException()
        {
        }

        public BasketAlreadyExistsException(string message) : base(message)
        {
        }

        public BasketAlreadyExistsException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}