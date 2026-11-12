using System;
using eCommerce.Storefront.UI.Web.MVC;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eCommerce.Storefront.Tests.Infrastructure
{
    [TestClass]
    public class ContentSecurityPolicyTests
    {
        [TestMethod]
        public void BuildContentSecurityPolicy_IncludesPerRequestNonce()
        {
            var policy = BootStrapper.BuildContentSecurityPolicy("nonce-value");

            StringAssert.Contains(policy, "script-src 'self' 'nonce-nonce-value'");
        }

        [TestMethod]
        public void BuildContentSecurityPolicy_ScriptSourceRejectsInlineAndEval()
        {
            var policy = BootStrapper.BuildContentSecurityPolicy("nonce-value");
            var scriptSource = PolicyDirective(policy, "script-src");

            // Inline scripts must be authorised by the nonce only; event handlers and
            // javascript: URLs were removed from the views.
            Assert.IsFalse(scriptSource.Contains("'unsafe-inline'", StringComparison.Ordinal));
            Assert.IsFalse(scriptSource.Contains("'unsafe-eval'", StringComparison.Ordinal));
        }

        [TestMethod]
        public void BuildContentSecurityPolicy_KeepsWasmEvalForBlazor()
        {
            var policy = BootStrapper.BuildContentSecurityPolicy("nonce-value");

            StringAssert.Contains(PolicyDirective(policy, "script-src"), "'wasm-unsafe-eval'");
        }

        [TestMethod]
        public void BuildContentSecurityPolicy_RejectsEmptyNonce()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => BootStrapper.BuildContentSecurityPolicy(null));
            Assert.ThrowsExactly<ArgumentException>(() => BootStrapper.BuildContentSecurityPolicy(string.Empty));
        }

        private static string PolicyDirective(string policy, string directive)
        {
            foreach (var part in policy.Split(';'))
            {
                var trimmed = part.Trim();

                if (trimmed.StartsWith(directive + " ", StringComparison.Ordinal))
                {
                    return trimmed;
                }
            }

            Assert.Fail($"Directive '{directive}' was not found in: {policy}");

            return string.Empty;
        }
    }
}
