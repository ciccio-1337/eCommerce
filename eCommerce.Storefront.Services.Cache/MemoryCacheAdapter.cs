#nullable enable
using System;
using Microsoft.Extensions.Caching.Memory;

namespace eCommerce.Storefront.Services.Cache
{
    public class MemoryCacheAdapter(IMemoryCache memoryCache) : ICacheStorage
    {
        private readonly IMemoryCache _memoryCache = memoryCache;

        // Default TTL can be overridden via appsettings.json in production.
        // 10 minutes is the fallback for development / unconfigured keys.
        private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(10);

        public void Remove(string key)
        {
            _memoryCache.Remove(key);
        }

        public void Store(string key, object data)
        {
            ArgumentNullException.ThrowIfNull(data);

            var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(DefaultTtl);

            _memoryCache.Set(key, data, options);
        }

        // Kept for backward compatibility with existing callers.
        // Returns default(T) for both "not cached" and "cached null" —
        // callers should prefer TryRetrieve where the distinction matters.
        public T Retrieve<T>(string storageKey)
        {
            return _memoryCache.Get<T>(storageKey) ?? default;
        }

        // Distinguishes "not in cache" (returns false) from "cached null" (returns true, out value null).
        // This enables negative caching (store null to mean "does not exist") and avoids
        // re-fetching on every request when a null was cached.
        public bool TryRetrieve<T>(string storageKey, out T? value)
        {
            if (_memoryCache.TryGetValue(storageKey, out value))
            {
                return true;
            }

            value = default;
            return false;
        }
    }
}