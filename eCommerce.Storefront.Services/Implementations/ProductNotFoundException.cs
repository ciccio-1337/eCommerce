namespace eCommerce.Storefront.Services.Implementations
{
    public class ProductNotFoundException() : System.Exception("The requested product does not exist.")
    {
    }
}
