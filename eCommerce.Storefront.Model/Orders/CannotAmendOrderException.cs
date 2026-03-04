using System;

namespace eCommerce.Storefront.Model.Orders
{
    [Serializable]
    public class CannotAmendOrderException : Exception
    {
        public CannotAmendOrderException()
        {
        }

        public CannotAmendOrderException(string message) : base(message)
        {
        }

        public CannotAmendOrderException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}