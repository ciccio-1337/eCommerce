using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using eCommerce.Storefront.UI.Web.MVC.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace eCommerce.Storefront.Tests.Infrastructure
{
    [TestClass]
    public class AdminBootstrapperTests
    {
        private const string AdminEmail = "admin@example.com";
        private const string AdminPassword = "StrongPassword123!";

        [TestMethod]
        public async Task StartAsync_WhenEmailBelongsToExistingAccount_DoesNotPromoteIt()
        {
            var userManager = CreateUserManager();
            var roleManager = CreateRoleManager();

            roleManager
                .Setup(x => x.FindByNameAsync("Admin"))
                .ReturnsAsync(new IdentityRole("Admin"));
            userManager
                .Setup(x => x.GetUsersInRoleAsync("Admin"))
                .ReturnsAsync(new List<IdentityUser>());
            userManager
                .Setup(x => x.FindByEmailAsync(AdminEmail))
                // An attacker registered the configured address before deployment.
                .ReturnsAsync(new IdentityUser { Id = "attacker", Email = AdminEmail });

            var bootstrapper = CreateBootstrapper(userManager, roleManager);

            await bootstrapper.StartAsync(CancellationToken.None);

            userManager.Verify(
                x => x.AddToRoleAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()),
                Times.Never);
            userManager.Verify(
                x => x.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()),
                Times.Never);
        }

        [TestMethod]
        public async Task StartAsync_WhenNoAccountExists_CreatesAndPromotesTheAdministrator()
        {
            var userManager = CreateUserManager();
            var roleManager = CreateRoleManager();

            roleManager
                .Setup(x => x.FindByNameAsync("Admin"))
                .ReturnsAsync(new IdentityRole("Admin"));
            userManager
                .Setup(x => x.GetUsersInRoleAsync("Admin"))
                .ReturnsAsync(new List<IdentityUser>());
            userManager
                .Setup(x => x.FindByEmailAsync(AdminEmail))
                .ReturnsAsync((IdentityUser)null);
            userManager
                .Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), AdminPassword))
                .ReturnsAsync(IdentityResult.Success);
            userManager
                .Setup(x => x.IsInRoleAsync(It.IsAny<IdentityUser>(), "Admin"))
                .ReturnsAsync(false);
            userManager
                .Setup(x => x.AddToRoleAsync(It.IsAny<IdentityUser>(), "Admin"))
                .ReturnsAsync(IdentityResult.Success);

            var bootstrapper = CreateBootstrapper(userManager, roleManager);

            await bootstrapper.StartAsync(CancellationToken.None);

            userManager.Verify(
                x => x.CreateAsync(It.Is<IdentityUser>(u => u.Email == AdminEmail), AdminPassword),
                Times.Once);
            userManager.Verify(
                x => x.AddToRoleAsync(It.Is<IdentityUser>(u => u.Email == AdminEmail), "Admin"),
                Times.Once);
        }

        [TestMethod]
        public async Task StartAsync_WhenAnAdministratorAlreadyExists_SkipsBootstrap()
        {
            var userManager = CreateUserManager();
            var roleManager = CreateRoleManager();

            roleManager
                .Setup(x => x.FindByNameAsync("Admin"))
                .ReturnsAsync(new IdentityRole("Admin"));
            userManager
                .Setup(x => x.GetUsersInRoleAsync("Admin"))
                .ReturnsAsync(new List<IdentityUser> { new() { Id = "existing-admin" } });

            var bootstrapper = CreateBootstrapper(userManager, roleManager);

            await bootstrapper.StartAsync(CancellationToken.None);

            userManager.Verify(x => x.FindByEmailAsync(It.IsAny<string>()), Times.Never);
            userManager.Verify(
                x => x.AddToRoleAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()),
                Times.Never);
        }

        private static AdminBootstrapper CreateBootstrapper(
            Mock<UserManager<IdentityUser>> userManager,
            Mock<RoleManager<IdentityRole>> roleManager)
        {
            var scope = new Mock<IServiceScope>();
            scope.SetupGet(x => x.ServiceProvider)
                .Returns(new FakeServiceProvider(userManager.Object, roleManager.Object));

            var scopeFactory = new Mock<IServiceScopeFactory>();
            scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);

            var configuration = new Mock<IConfiguration>();
            configuration.Setup(x => x["Admin:Email"]).Returns(AdminEmail);
            configuration.Setup(x => x["Admin:Password"]).Returns(AdminPassword);

            return new AdminBootstrapper(
                scopeFactory.Object,
                configuration.Object,
                Mock.Of<ILogger<AdminBootstrapper>>());
        }

        private sealed class FakeServiceProvider(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager) : IServiceProvider
        {
            public object GetService(Type serviceType)
            {
                if (serviceType == typeof(UserManager<IdentityUser>))
                {
                    return userManager;
                }

                if (serviceType == typeof(RoleManager<IdentityRole>))
                {
                    return roleManager;
                }

                return null;
            }
        }

        private static Mock<UserManager<IdentityUser>> CreateUserManager() =>
            new(
                Mock.Of<IUserStore<IdentityUser>>(),
                null,
                new PasswordHasher<IdentityUser>(),
                null,
                null,
                null,
                null,
                null,
                null);

        private static Mock<RoleManager<IdentityRole>> CreateRoleManager() =>
            new(
                Mock.Of<IRoleStore<IdentityRole>>(),
                null,
                null,
                null,
                null);
    }
}
