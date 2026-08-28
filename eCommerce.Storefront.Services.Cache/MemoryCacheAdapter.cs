using Microsoft.Extensions.Caching.Memory;

namespace eCommerce.Storefront.Services.Cache
{
    public class MemoryCacheAdapter(IMemoryCache memoryCache) : ICacheStorage
    {
        private readonly IMemoryCache _memoryCache = memoryCache;

        public void Remove(string key)
        {
            _memoryCache.Remove(key);
        }

        public void Store(string key, object data)
        {
            _memoryCache.Set(key, data);
        }

        public T Retrieve<T>(string storageKey)
        {
            T itemStored = _memoryCache.Get<T>(storageKey) ?? default;

            return itemStored;
        }
    }
}