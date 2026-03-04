using System;

namespace eCommerce.Storefront.Services.Implementations
{
    [Serializable]
    public class BasketDoesNotExistException : Exception
    {
        public BasketDoesNotExistException()
        {
        }

        public BasketDoesNotExistException(string message) : base(message)
        {
        }

        public BasketDoesNotExistException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}