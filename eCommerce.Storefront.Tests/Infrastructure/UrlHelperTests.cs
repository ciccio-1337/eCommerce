using System;
using eCommerce.Storefront.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace eCommerce.Storefront.Tests.Infrastructure
{
    [TestClass]
    public class UrlHelperTests
    {
        [TestMethod]
        public void Resolve_UsesConfiguredPublicBaseUrl()
        {
            var context = CreateContext(Environments.Production, "https://shop.example.com");

            Assert.AreEqual(
                "https://shop.example.com/order/detail",
                context.Resolve("order/detail"));
        }

        [TestMethod]
        public void Resolve_OutsideDevelopmentWithoutBaseUrl_Throws()
        {
            var context = CreateContext(Environments.Production, null);

            Assert.ThrowsExactly<InvalidOperationException>(() => context.Resolve("order/detail"));
        }

        [TestMethod]
        public void Resolve_InDevelopmentWithoutBaseUrl_FallsBackToRequestHost()
        {
            var context = CreateContext(Environments.Development, null);

            Assert.AreEqual(
                "https://localhost/order/detail",
                context.Resolve("/order/detail"));
        }

        private static HttpContext CreateContext(string environmentName, string publicBaseUrl)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(publicBaseUrl is null
                    ? []
                    : new System.Collections.Generic.Dictionary<string, string>
                    {
                        ["App:PublicBaseUrl"] = publicBaseUrl
                    })
                .Build();

            var environment = new Mock<IHostEnvironment>();
            environment.SetupGet(x => x.EnvironmentName).Returns(environmentName);

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton(environment.Object);

            return new DefaultHttpContext
            {
                RequestServices = services.BuildServiceProvider(),
                Request =
                {
                    Scheme = "https",
                    Host = new HostString("localhost")
                }
            };
        }
    }
}
