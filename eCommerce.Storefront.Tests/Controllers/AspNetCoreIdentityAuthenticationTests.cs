using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using eCommerce.Storefront.Controllers.Services.Implementations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace eCommerce.Storefront.Tests.Controllers
{
    [TestClass]
    public class AspNetCoreIdentityAuthenticationTests
    {
        [TestMethod]
        public async Task LoginAsync_WhenTwoFactorEnabled_FailsClosed()
        {
            var identityUser = new IdentityUser { Id = "user-1", Email = "user@test.com" };
            var userManager = CreateUserManager();
            var signInManager = CreateSignInManager(userManager);

            userManager.Setup(x => x.FindByEmailAsync("user@test.com")).ReturnsAsync(identityUser);
            signInManager
                .Setup(x => x.CheckPasswordSignInAsync(identityUser, "password", true))
                .ReturnsAsync(SignInResult.Success);
            userManager.Setup(x => x.GetTwoFactorEnabledAsync(identityUser)).ReturnsAsync(true);

            var sut = new AspNetCoreIdentityAuthentication(
                signInManager.Object,
                Mock.Of<ILogger<AspNetCoreIdentityAuthentication>>());

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => sut.LoginAsync("user@test.com", "password"));
        }

        [TestMethod]
        public async Task LoginAsync_WhenTwoFactorDisabled_ReturnsAuthenticatedUser()
        {
            var identityUser = new IdentityUser { Id = "user-1", Email = "user@test.com" };
            var userManager = CreateUserManager();
            var signInManager = CreateSignInManager(userManager);

            userManager.Setup(x => x.FindByEmailAsync("user@test.com")).ReturnsAsync(identityUser);
            signInManager
                .Setup(x => x.CheckPasswordSignInAsync(identityUser, "password", true))
                .ReturnsAsync(SignInResult.Success);
            userManager.Setup(x => x.GetTwoFactorEnabledAsync(identityUser)).ReturnsAsync(false);
            userManager.Setup(x => x.IsEmailConfirmedAsync(identityUser)).ReturnsAsync(true);
            userManager
                .Setup(x => x.GetRolesAsync(identityUser))
                .ReturnsAsync(new List<string> { "Customer" });

            var sut = new AspNetCoreIdentityAuthentication(
                signInManager.Object,
                Mock.Of<ILogger<AspNetCoreIdentityAuthentication>>());

            var result = await sut.LoginAsync("user@test.com", "password");

            Assert.IsTrue(result.IsAuthenticated);
            Assert.AreEqual("user-1", result.Id);
            Assert.AreEqual("user@test.com", result.Email);
            CollectionAssert.Contains(new List<string>(result.Roles), "Customer");
        }

        [TestMethod]
        public async Task LoginAsync_WithUnknownAccount_Throws()
        {
            var userManager = CreateUserManager();
            var signInManager = CreateSignInManager(userManager);

            userManager
                .Setup(x => x.FindByEmailAsync("missing@test.com"))
                .ReturnsAsync((IdentityUser)null);

            var sut = new AspNetCoreIdentityAuthentication(
                signInManager.Object,
                Mock.Of<ILogger<AspNetCoreIdentityAuthentication>>());

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => sut.LoginAsync("missing@test.com", "password"));
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

        private static Mock<SignInManager<IdentityUser>> CreateSignInManager(
            Mock<UserManager<IdentityUser>> userManager) =>
            new(
                userManager.Object,
                Mock.Of<IHttpContextAccessor>(),
                Mock.Of<IUserClaimsPrincipalFactory<IdentityUser>>(),
                null,
                null,
                null,
                null);
    }
}
