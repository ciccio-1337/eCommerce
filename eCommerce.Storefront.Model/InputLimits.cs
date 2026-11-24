namespace eCommerce.Storefront.Model
{
    /// <summary>
    /// Central upper bounds for user-supplied text. They mirror the database
    /// column sizes and the Identity limits so an oversized value is rejected by
    /// validation instead of reaching the password hasher or the database.
    /// </summary>
    public static class InputLimits
    {
        public const int MaxEmailLength = 256;
        public const int MaxPasswordLength = 128;
        public const int MinPasswordLength = 8;
        public const int MaxNameLength = 100;
        public const int MaxAddressFieldLength = 50;
    }
}
