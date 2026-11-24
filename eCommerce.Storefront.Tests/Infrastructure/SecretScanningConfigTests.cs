using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eCommerce.Storefront.Tests.Infrastructure
{
    [TestClass]
    public class SecretScanningConfigTests
    {
        [TestMethod]
        public void GitleaksConfig_DoesNotContainBurnedCredentials()
        {
            var config = File.ReadAllText(FindGitleaksConfig());

            // The values are assembled at runtime so this test file does not
            // itself contain the burned credentials it is checking for.
            Assert.IsFalse(config.Contains("Ciccio_" + "110590", StringComparison.Ordinal));
            Assert.IsFalse(config.Contains("YawZitNzuASTzARiZIJpFhHGAQ8MVq9gK2BHDYm9cvY" + "=", StringComparison.Ordinal));
            Assert.IsFalse(config.Contains(
                "f260761961044d299a49b608b898e62e3ed0e5a0b96046ecbbc3a1598e32f3" + "a3",
                StringComparison.Ordinal));
            Assert.IsFalse(config.Contains(
                "3d5bc7f9-e445-46fe-b284-894c3f0a" + "1873",
                StringComparison.Ordinal));
        }

        [TestMethod]
        public void GitleaksConfig_ExtendsDefaultRules()
        {
            var config = File.ReadAllText(FindGitleaksConfig());

            StringAssert.Contains(config, "useDefault = true");
        }

        private static string FindGitleaksConfig()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null)
            {
                var candidate = Path.Combine(directory.FullName, ".gitleaks.toml");

                if (File.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            Assert.Fail(".gitleaks.toml was not found above the test output directory.");

            return null;
        }
    }
}
