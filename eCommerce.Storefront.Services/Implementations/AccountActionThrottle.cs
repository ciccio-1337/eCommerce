using System;
using System.Collections.Concurrent;
using eCommerce.Storefront.Services.Interfaces;

namespace eCommerce.Storefront.Services.Implementations
{
    /// <summary>
    /// In-memory, process-local cooldown. A multi-instance deployment should
    /// replace this with a shared store (for example Redis) so the cooldown is
    /// global; the interface keeps that swap local.
    /// </summary>
    public sealed class AccountActionThrottle : IAccountActionThrottle
    {
        private readonly ConcurrentDictionary<string, DateTimeOffset> _lastAttempts =
            new(StringComparer.OrdinalIgnoreCase);

        public bool TryAcquire(string accountKey, TimeSpan cooldown)
        {
            if (string.IsNullOrWhiteSpace(accountKey))
            {
                return false;
            }

            var now = DateTimeOffset.UtcNow;
            var acquired = false;

            _lastAttempts.AddOrUpdate(
                accountKey,
                _ =>
                {
                    acquired = true;

                    return now;
                },
                (_, lastAttempt) =>
                {
                    if (now - lastAttempt < cooldown)
                    {
                        return lastAttempt;
                    }

                    acquired = true;

                    return now;
                });

            // Bound the dictionary so an attacker cannot grow it without limit by
            // cycling addresses. Expired entries are cheap to remove.
            if (_lastAttempts.Count > 10_000)
            {
                foreach (var entry in _lastAttempts)
                {
                    if (now - entry.Value >= cooldown)
                    {
                        _lastAttempts.TryRemove(entry.Key, out _);
                    }
                }
            }

            return acquired;
        }
    }
}
