using System;
using System.Linq;
using System.Text;
using eCommerce.Storefront.Repository.EntityFrameworkCore;
using eCommerce.Storefront.Services;
using eCommerce.Storefront.UI.Web.MVC;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAutoMapper(typeof(AutoMapperBootStrapper));
builder.Services.AddHttpContextAccessor();
builder.Services.AddDbContext<ShopDataContext>(options => 
{
    if (builder.Environment.IsDevelopment())
    {
        options.UseSqlite(builder.Configuration?.GetConnectionString("DefaultConnection"), b => 
        {
            b.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            b.MigrationsAssembly("eCommerce.Storefront.Repository.EntityFrameworkCore");
        });
    }
    else
    {
        options.UseNpgsql(builder.Configuration?.GetConnectionString("DefaultConnection"), b => 
        {
            b.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            b.MigrationsAssembly("eCommerce.Storefront.Repository.EntityFrameworkCore");
        });
    }

    options.ConfigureWarnings(warningsConfigurationBuilderAction => 
    {
        warningsConfigurationBuilderAction.Ignore(RelationalEventId.AmbientTransactionWarning);
    });
});
builder.Services.AddDefaultIdentity<IdentityUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ShopDataContext>();
builder.Services.Configure<IdentityOptions>(options =>
{
    // Password settings.
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;
    // Lockout settings.
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    // User settings.
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    // Cookie settings
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(double.Parse(builder.Configuration["CookieAuthenticationTimeout"]));
    options.LoginPath = "/AccountLogOn/LogOn";
    options.AccessDeniedPath = "/AccountRegister/Register";
    options.SlidingExpiration = true;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecurityKey"]))
    };
});
builder.Services.AddControllersWithViews(options => 
{
    options.ModelBinderProviders.RemoveType<DateTimeModelBinderProvider>();
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
}).AddNewtonsoftJson();
builder.Services.ConfigureDependencies();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});
builder.Services.AddLogging(configure => 
{
    configure.AddConfiguration(builder.Configuration.GetSection("Logging"));
    configure.AddConsole();
    configure.AddDebug();
    configure.AddEventSourceLogger();
});

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
var localizationOptions = new RequestLocalizationOptions().SetDefaultCulture(supportedCultures[0]).AddSupportedCultures(supportedCultures).AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    if (!context.Response.Headers.ContainsKey("X-content-type-options"))
    {
        context.Response.Headers.Append("X-content-type-options", "nosniff");
    }

    if (!context.Response.Headers.ContainsKey("Cache-control"))
    {
        context.Response.Headers.Append("Cache-control", "no-cache, no-store");
    }

    if (!context.Response.Headers.ContainsKey("Pragma"))
    {
        context.Response.Headers.Append("Pragma", "no-cache");
    }

    if (!context.Response.Headers.ContainsKey("X-XSS-Protection"))
    {
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    }

    if (context.Request.Path.Value.Contains("/admin"))
    {
        context.Response.Redirect($"{context.Request.Scheme}://{context.Request.Host.Value}/index.html");
    
        return;
    }

    await next();
});
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Logger.LogInformation("Application Started");
app.Run();