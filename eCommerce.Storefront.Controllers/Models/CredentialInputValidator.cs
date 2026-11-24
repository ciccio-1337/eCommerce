using eCommerce.Storefront.Model;

namespace eCommerce.Storefront.Controllers.Models
{
    /// <summary>
    /// Length checks for values that are passed to ASP.NET Identity before the
    /// framework gets a chance to validate them. Identity's password hasher runs
    /// PBKDF2 over the whole input, so a multi-megabyte password is a cheap way
    /// to burn CPU; an over-long email or name otherwise reaches the database and
    /// surfaces as an unhandled 500.
    /// </summary>
    public static class CredentialInputValidator
    {
        public static bool IsAcceptableEmail(string email) =>
            !string.IsNullOrWhiteSpace(email)
            && email.Length <= InputLimits.MaxEmailLength;

        public static bool IsAcceptablePassword(string password) =>
            !string.IsNullOrEmpty(password)
            && password.Length <= InputLimits.MaxPasswordLength;

        public static bool IsAcceptableName(string name) =>
            name == null || name.Length <= InputLimits.MaxNameLength;

        public static bool IsAcceptablePasswordForVerification(string password) =>
            password == null || password.Length <= InputLimits.MaxPasswordLength;

        public static bool IsAcceptableEmailForLookup(string email) =>
            email == null || email.Length <= InputLimits.MaxEmailLength;

        /// <summary>
        /// Returns a user-facing message for the first failed field, or
        /// <c>null</c> when the values are acceptable. The messages deliberately
        /// do not echo the submitted value.
        /// </summary>
        public static string ValidateRegistration(string email, string password, string firstName, string secondName)
        {
            if (!IsAcceptableEmail(email))
            {
                return $"An email address must be at most {InputLimits.MaxEmailLength} characters.";
            }

            if (!IsAcceptablePassword(password))
            {
                return $"A password must be at most {InputLimits.MaxPasswordLength} characters.";
            }

            if (!IsAcceptableName(firstName) || !IsAcceptableName(secondName))
            {
                return $"A name must be at most {InputLimits.MaxNameLength} characters.";
            }

            return null;
        }
    }
}
