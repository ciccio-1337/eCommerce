using eCommerce.Storefront.Controllers.ActionArguments;
using eCommerce.Storefront.Services.Implementations;
using eCommerce.Storefront.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using eCommerce.Storefront.Services.Cache;
using eCommerce.Storefront.Repository.EntityFrameworkCore;
using eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Interfaces;
using eCommerce.Storefront.Repository.EntityFrameworkCore.Repositories.Implementations;
using eCommerce.Backoffice.Shared.Services.Interfaces;
using eCommerce.Backoffice.Shared.Services.Implementations;
using eCommerce.Storefront.Controllers.Services.Interfaces;
using eCommerce.Storefront.Controllers.Services.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using eCommerce.Storefront.Services;
using Microsoft.AspNetCore.Identity;
using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Net.Http.Headers;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace eCommerce.Storefront.UI.Web.MVC
{
    public static class BootStrapper
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection serviceCollection, IConfiguration configuration, IHostEnvironment hostEnvironment)
        {
            serviceCollection.AddAutoMapper(typeof(AutoMapperBootStrapper));
            serviceCollection.AddHttpContextAccessor();
            serviceCollection.AddDbContext<ShopDataContext>(options => 
            {
                if (hostEnvironment.IsDevelopment())
                {
                    options.UseSqlite(configuration.GetConnectionString("DefaultConnection"), b => 
                    {
                        b.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                        b.MigrationsAssembly("eCommerce.Storefront.Repository.EntityFrameworkCore");
                    });
                }
                else
                {
                    options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"), b => 
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
            serviceCollection.AddIdentityCore<IdentityUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<ShopDataContext>().AddSignInManager();
            serviceCollection.Configure<IdentityOptions>(options =>
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
            serviceCollection.AddAuthentication().AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                // Cookie settings
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(double.Parse(configuration["CookieAuthenticationTimeout"]));
                options.LoginPath = "/AccountLogOn/LogOn";
                options.AccessDeniedPath = "/AccountLogOn/LogOn";
                options.SlidingExpiration = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            }).AddCookie(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                // Cookie settings
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(double.Parse(configuration["CookieAuthenticationTimeout"]));
                options.LoginPath = "/admin/account/login";
                options.AccessDeniedPath = "/admin/account/login";
                options.SlidingExpiration = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });
            serviceCollection.AddAntiforgery();
            serviceCollection.AddLogging(configure => 
            {
                configure.AddConfiguration(configuration.GetSection("Logging"));
                configure.AddConsole();
                configure.AddDebug();
                configure.AddEventSourceLogger();
            });
            serviceCollection.AddHttpClient<IPaymentService, PayPalPaymentService>();

            return serviceCollection;
        }

        public static IServiceCollection AddRepositories(this IServiceCollection serviceCollection)
        {
            // Repositories
            serviceCollection.AddScoped<IUnitOfWork, UnitOfWork>();
            serviceCollection.AddScoped(typeof(IReadOnlyRepository<,>), typeof(Repository<,>));
            serviceCollection.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
            serviceCollection.AddScoped<IBasketRepository, BasketRepository>();
            serviceCollection.AddScoped<IProductTitleRepository, ProductTitleRepository>();
            serviceCollection.AddScoped<IProductRepository, ProductRepository>();
            serviceCollection.AddScoped<ICustomerRepository, CustomerRepository>();
            serviceCollection.AddScoped<IOrderRepository, OrderRepository>();
            serviceCollection.AddScoped<IDeliveryOptionRepository, DeliveryOptionRepository>();

            return serviceCollection;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection serviceCollection)
        {
            // Product Catalogue
            serviceCollection.AddScoped(typeof(IEntityService<,>), typeof(EntityService<,>));
            serviceCollection.AddScoped<IProductCatalogService, ProductCatalogService>();
            serviceCollection.AddScoped<IBasketService, BasketService>();
            serviceCollection.AddScoped<ICookieStorageService, CookieStorageService>();
            serviceCollection.AddScoped<ICustomerService, CustomerService>();
            // Order Service
            serviceCollection.AddScoped<IOrderService, OrderService>();
            // Authentication
            serviceCollection.AddScoped<ICookieAuthentication, AspNetCoreCookieAuthentication>();
            serviceCollection.AddScoped<ILocalAuthenticationService, AspNetCoreIdentityAuthentication>();
            // Controller Helpers
            serviceCollection.AddScoped<IActionArguments, HttpRequestActionArguments>();
            // Payment
            serviceCollection.AddScoped<IPaymentService, PayPalPaymentService>();
            // Caching Strategies
            serviceCollection.AddScoped<ICacheStorage, MemoryCacheAdapter>();
            serviceCollection.AddScoped<ICachedProductCatalogService, CachedProductCatalogService>();
            // Email
            serviceCollection.AddScoped<IEmailService, SmtpService>();

            return serviceCollection;
        }

        public static WebApplication UseSecurityHeaders(this WebApplication webApplication)
        {
            webApplication.Use(async (context, next) =>
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

            return webApplication;
        }
    }
}