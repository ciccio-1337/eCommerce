using eCommerce.Storefront.UI.Web.MVC;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace eCommerce.Storefront.Tests.Infrastructure
{
    [TestClass]
    public class SecurityHeaderTests
    {
        [TestMethod]
        [DataRow("/_framework/dotnet.wasm")]
        [DataRow("/_framework/blazor.boot.json")]
        [DataRow("/css/site.css")]
        [DataRow("/js/site.js")]
        [DataRow("/content/images/products/1.jpg")]
        [DataRow("/favicon.ico")]
        public void IsStaticAssetRequest_ReturnsTrueForStaticAssets(string path)
        {
            Assert.IsTrue(BootStrapper.IsStaticAssetRequest(new PathString(path)));
        }

        [TestMethod]
        [DataRow("/")]
        [DataRow("/Customer/Detail")]
        [DataRow("/AccountLogOn/LogOn")]
        [DataRow("/api/accounts/login")]
        public void IsStaticAssetRequest_ReturnsFalseForDynamicResponses(string path)
        {
            Assert.IsFalse(BootStrapper.IsStaticAssetRequest(new PathString(path)));
        }

        [TestMethod]
        public void IsStaticAssetRequest_WithEmptyPath_ReturnsFalse()
        {
            Assert.IsFalse(BootStrapper.IsStaticAssetRequest(PathString.Empty));
        }

        [TestMethod]
        public void ApplyStaticAssetCacheHeaders_BlazorFrameworkFilesAreRevalidatedHourly()
        {
            var context = new DefaultHttpContext();
            context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
            context.Response.Headers.Pragma = "no-cache";
            context.Response.Headers.Expires = "0";

            BootStrapper.ApplyStaticAssetCacheHeaders(context.Response, new PathString("/_framework/dotnet.wasm"));

            Assert.AreEqual("public, max-age=3600, must-revalidate", context.Response.Headers.CacheControl.ToString());
            Assert.IsFalse(context.Response.Headers.ContainsKey(HeaderNames.Pragma));
            Assert.IsFalse(context.Response.Headers.ContainsKey(HeaderNames.Expires));
        }

        [TestMethod]
        public void ApplyStaticAssetCacheHeaders_WwwRootFilesAreCachedForAWeek()
        {
            var context = new DefaultHttpContext();

            BootStrapper.ApplyStaticAssetCacheHeaders(context.Response, new PathString("/css/site.css"));

            Assert.AreEqual("public, max-age=604800", context.Response.Headers.CacheControl.ToString());
        }
    }
}
