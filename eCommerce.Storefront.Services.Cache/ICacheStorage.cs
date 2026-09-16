#nullable enable
namespace eCommerce.Storefront.Services.Cache
{
    public interface ICacheStorage
    {
        void Remove(string key);
        void Store(string key, object data);
        T Retrieve<T>(string storageKey);

        // Distinguishes "not cached" from "cached null" to enable negative caching.
        bool TryRetrieve<T>(string storageKey, out T? value);
    }
}
#nullable restore