using System;

namespace eCommerce.Storefront.Model
{
    [Serializable]
    public class ValueObjectIsInvalidException : Exception
    {
        public ValueObjectIsInvalidException()
        {
        }

        public ValueObjectIsInvalidException(string message) : base(message)
        {
        }

        public ValueObjectIsInvalidException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}