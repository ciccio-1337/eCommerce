using System.Linq;
using eCommerce.Storefront.UI.Web.MVC;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.AddRepositories();
builder.Services.AddApplicationServices();
builder.Services.AddControllersWithViews(options => 
{
    options.ModelBinderProviders.RemoveType<DateTimeModelBinderProvider>();
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
}).AddNewtonsoftJson();

var app = builder.Build();

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
app.UseBlazorFrameworkFiles();            
app.UseStaticFiles();

var supportedCultures = new[] { "en-GB", "en-US", "it-IT" };
var localizationOptions = new RequestLocalizationOptions().SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    if (!context.Response.Headers.ContainsKey(HeaderNames.XContentTypeOptions))
    {
        context.Response.Headers.Append(HeaderNames.XContentTypeOptions, "nosniff");
    }

    if (!context.Response.Headers.ContainsKey(HeaderNames.XFrameOptions))
    {
        context.Response.Headers.Append(HeaderNames.XFrameOptions, "DENY");
    }

    if (!context.Response.Headers.ContainsKey("Referrer-Policy"))
    {
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    }

    if (!context.Response.Headers.ContainsKey(HeaderNames.CacheControl))
    {
        context.Response.Headers.Append(HeaderNames.CacheControl, "no-cache, no-store, must-revalidate");
    }

    if (!context.Response.Headers.ContainsKey(HeaderNames.Pragma))
    {
        context.Response.Headers.Append(HeaderNames.Pragma, "no-cache");
    }

    if (!context.Response.Headers.ContainsKey(HeaderNames.Expires))
    {
        context.Response.Headers.Append(HeaderNames.Expires, "0");
    }

    if (context.Request.Path.StartsWithSegments("/admin"))
    {
        context.Response.Redirect("/index.html");

        return;
    }

    await next();
});
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Logger.LogInformation("Application Started");
app.Run();