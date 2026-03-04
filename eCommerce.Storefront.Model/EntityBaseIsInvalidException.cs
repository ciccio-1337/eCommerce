using System;

namespace eCommerce.Storefront.Model
{
    [Serializable]
    public class EntityBaseIsInvalidException : Exception
    {
        public EntityBaseIsInvalidException()
        {
        }

        public EntityBaseIsInvalidException(string message) : base(message)
        {
        }

        public EntityBaseIsInvalidException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}