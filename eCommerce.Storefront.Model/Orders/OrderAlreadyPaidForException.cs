using System;

namespace eCommerce.Storefront.Model.Orders
{
    [Serializable]
    public class OrderAlreadyPaidForException : Exception
    {
        public OrderAlreadyPaidForException()
        {
        }

        public OrderAlreadyPaidForException(string message) : base(message)
        {
        }

        public OrderAlreadyPaidForException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}