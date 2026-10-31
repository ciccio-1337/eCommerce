using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.RateLimiting;
using eCommerce.Storefront.UI.Web.MVC;
using eCommerce.Storefront.UI.Web.MVC.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Fail closed if the host allow-list was left wide open outside development.
if (!builder.Environment.IsDevelopment())
{
    var allowedHosts = builder.Configuration["AllowedHosts"];

    if (string.IsNullOrWhiteSpace(allowedHosts) || allowedHosts.Trim() == "*")
    {
        throw new InvalidOperationException(
            "AllowedHosts must be configured to an explicit host list in non-development environments.");
    }

    var publicBaseUrl = builder.Configuration["App:PublicBaseUrl"];

    if (string.IsNullOrWhiteSpace(publicBaseUrl)
        || !Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var publicBaseUri)
        || publicBaseUri.Scheme != Uri.UriSchemeHttps)
    {
        throw new InvalidOperationException(
            "App:PublicBaseUrl must be configured to an absolute HTTPS URL in non-development environments.");
    }
}

builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddRepositories();
builder.Services.AddApplicationServices();

// Persist data-protection keys so cookies and antiforgery tokens survive restarts
// and are shared by every instance behind the load balancer.
var keysPath = builder.Configuration["DataProtection:KeysPath"] ?? "keys";
var keysDirectory = new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, keysPath));
keysDirectory.Create();

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(keysDirectory)
    .SetApplicationName("eCommerce");

// Trust forwarded headers only from configured proxies. When no proxy is
// configured the framework defaults (loopback) remain in place.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    var proxies = builder.Configuration.GetSection("ReverseProxy:KnownProxies")
        .GetChildren()
        .Select(section => section.Value)
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .ToArray();

    var networks = builder.Configuration.GetSection("ReverseProxy:KnownNetworks")
        .GetChildren()
        .Select(section => section.Value)
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .ToArray();

    if (proxies.Length > 0 || networks.Length > 0)
    {
        options.KnownProxies.Clear();
        options.KnownIPNetworks.Clear();

        foreach (var proxy in proxies)
        {
            if (IPAddress.TryParse(proxy, out var address))
            {
                options.KnownProxies.Add(address);
            }
        }

        foreach (var network in networks)
        {
            if (System.Net.IPNetwork.TryParse(network, out var ipNetwork))
            {
                options.KnownIPNetworks.Add(ipNetwork);
            }
        }
    }
});

// Throttle credential endpoints by client IP. This complements Identity's
// per-account lockout and makes online guessing and lockout abuse expensive.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("registration", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddHostedService<AdminBootstrapper>();

builder.Services.AddControllersWithViews(options =>
{
    options.ModelBinderProviders.RemoveType<DateTimeModelBinderProvider>();
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
}).AddNewtonsoftJson();

var app = builder.Build();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseSecurityHeaders();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

var supportedCultures = new[] { "en-GB", "en-US", "it-IT" };
var localizationOptions = new RequestLocalizationOptions().SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapFallbackToFile("index.html");
app.Logger.LogInformation("Application Started");
app.Run();
