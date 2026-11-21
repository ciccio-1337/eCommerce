using System;
using System.Collections.Generic;
using System.Threading;
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

        [TestMethod]
        public async Task ValidatePasswordAsync_UsesLockoutAwarePasswordCheck()
        {
            var identityUser = new IdentityUser { Id = "user-1", Email = "user@test.com" };
            var userManager = CreateUserManager();
            var signInManager = CreateSignInManager(userManager);

            userManager.Setup(x => x.FindByEmailAsync("user@test.com")).ReturnsAsync(identityUser);
            signInManager
                .Setup(x => x.CheckPasswordSignInAsync(identityUser, "wrong", true))
                .ReturnsAsync(SignInResult.Failed);

            var sut = new AspNetCoreIdentityAuthentication(
                signInManager.Object,
                Mock.Of<ILogger<AspNetCoreIdentityAuthentication>>());

            var result = await sut.ValidatePasswordAsync("user@test.com", "wrong");

            Assert.IsFalse(result);
            signInManager.Verify(
                x => x.CheckPasswordSignInAsync(identityUser, "wrong", true),
                Times.Once);
            userManager.Verify(
                x => x.CheckPasswordAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()),
                Times.Never);
        }

        [TestMethod]
        public async Task RegisterUserAsync_WhenRoleAssignmentFails_DeletesTheCreatedUser()
        {
            var store = new Mock<IUserStore<IdentityUser>>();
            store
                .Setup(x => x.UpdateAsync(It.IsAny<IdentityUser>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IdentityResult.Success);
            store
                .Setup(x => x.DeleteAsync(It.IsAny<IdentityUser>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(IdentityResult.Success);

            var userManager = CreateUserManager(store);
            var signInManager = CreateSignInManager(userManager);

            userManager
                .Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), "password"))
                .Callback<IdentityUser, string>((user, _) => user.Id = "created-user")
                .ReturnsAsync(IdentityResult.Success);
            userManager
                .Setup(x => x.AddToRoleAsync(It.IsAny<IdentityUser>(), "Customer"))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError
                {
                    Code = "RoleFailure",
                    Description = "The role could not be assigned."
                }));

            var sut = new AspNetCoreIdentityAuthentication(
                signInManager.Object,
                Mock.Of<ILogger<AspNetCoreIdentityAuthentication>>());

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
                sut.RegisterUserAsync("user@test.com", "password", false, ["Customer"]));

            userManager.Verify(x => x.DeleteAsync(It.IsAny<IdentityUser>()), Times.Once);
        }

        private static Mock<UserManager<IdentityUser>> CreateUserManager(
            Mock<IUserStore<IdentityUser>> store = null) =>
            new(
                store?.Object ?? Mock.Of<IUserStore<IdentityUser>>(),
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
