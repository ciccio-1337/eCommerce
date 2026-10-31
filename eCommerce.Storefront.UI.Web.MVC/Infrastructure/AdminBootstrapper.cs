using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace eCommerce.Storefront.UI.Web.MVC.Infrastructure
{
    /// <summary>
    /// Creates the first administrator from configuration when the database does
    /// not contain one. No administrator credential is ever shipped in the
    /// repository: set <c>Admin:Email</c> and <c>Admin:Password</c> (preferably via
    /// environment variables or a secret store) on first deployment.
    /// </summary>
    public sealed class AdminBootstrapper(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<AdminBootstrapper> logger) : IHostedService
    {
        private const string AdminRoleName = "Admin";

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var email = configuration["Admin:Email"];
            var password = configuration["Admin:Password"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                logger.LogInformation(
                    "Admin bootstrap skipped: Admin:Email and Admin:Password are not configured.");

                return;
            }

            if (password.Length < 12 || password.Length > 128 || IsKnownDefault(password))
            {
                logger.LogError(
                    "Admin bootstrap refused: the configured administrator password is too short, too long, or is a known default.");

                return;
            }

            using var scope = scopeFactory.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            try
            {
                var adminRole = await roleManager.FindByNameAsync(AdminRoleName);

                if (adminRole is null)
                {
                    adminRole = new IdentityRole(AdminRoleName);
                    var roleResult = await roleManager.CreateAsync(adminRole);

                    if (!roleResult.Succeeded)
                    {
                        logger.LogError(
                            "Admin bootstrap failed to create the Admin role: {Errors}",
                            string.Join("; ", roleResult.Errors.Select(error => error.Description)));

                        return;
                    }
                }

                var existingAdmins = await userManager.GetUsersInRoleAsync(AdminRoleName);

                if (existingAdmins.Count > 0)
                {
                    logger.LogInformation(
                        "Admin bootstrap skipped: an administrator account already exists.");

                    return;
                }

                var user = await userManager.FindByEmailAsync(email);

                if (user is null)
                {
                    user = new IdentityUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true
                    };

                    var createResult = await userManager.CreateAsync(user, password);

                    if (!createResult.Succeeded)
                    {
                        logger.LogError(
                            "Admin bootstrap failed to create the administrator: {Errors}",
                            string.Join("; ", createResult.Errors.Select(error => error.Description)));

                        return;
                    }
                }
                else if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                    await userManager.UpdateAsync(user);
                }

                if (!await userManager.IsInRoleAsync(user, AdminRoleName))
                {
                    await userManager.AddToRoleAsync(user, AdminRoleName);
                }

                logger.LogInformation("Admin bootstrap created the initial administrator account.");
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Admin bootstrap failed. Ensure the database is migrated and reachable before relying on it.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private static bool IsKnownDefault(string password) =>
            password.Equals("Admin-1234", StringComparison.OrdinalIgnoreCase)
            || password.Equals("Password123!", StringComparison.OrdinalIgnoreCase)
            || password.Equals("ChangeMe123!", StringComparison.OrdinalIgnoreCase);
    }
}
