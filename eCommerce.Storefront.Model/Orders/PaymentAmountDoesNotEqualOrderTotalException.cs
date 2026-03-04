using System;

namespace eCommerce.Storefront.Model.Orders
{
    [Serializable]
    public class PaymentAmountDoesNotEqualOrderTotalException : Exception
    {
        public PaymentAmountDoesNotEqualOrderTotalException()
        {
        }

        public PaymentAmountDoesNotEqualOrderTotalException(string message) : base(message)
        {
        }

        public PaymentAmountDoesNotEqualOrderTotalException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}