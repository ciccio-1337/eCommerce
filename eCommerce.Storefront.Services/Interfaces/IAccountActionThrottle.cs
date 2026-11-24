using System;

namespace eCommerce.Storefront.Services.Interfaces
{
    /// <summary>
    /// Per-account cooldown for actions that send mail (password reset,
    /// confirmation resend). The per-IP rate limiter cannot stop a distributed
    /// attacker from mail-bombing one address, so the address itself is the key.
    /// </summary>
    public interface IAccountActionThrottle
    {
        /// <summary>
        /// Returns <c>true</c> when the action may run and records the attempt;
        /// <c>false</c> while the cooldown for that account is still active.
        /// </summary>
        bool TryAcquire(string accountKey, TimeSpan cooldown);
    }
}
