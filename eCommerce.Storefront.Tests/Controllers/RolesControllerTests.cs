using System.Security.Claims;
using System.Threading.Tasks;
using eCommerce.Backoffice.Server.Controllers;
using eCommerce.Backoffice.Shared.Model.Roles;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace eCommerce.Storefront.Tests.Controllers
{
    [TestClass]
    public class RolesControllerTests
    {
        private const string CurrentUserId = "admin-1";
        private const string RoleId = "role-admin";

        private Mock<RoleManager<IdentityRole>> _roleManagerMock;
        private Mock<UserManager<IdentityUser>> _userManagerMock;
        private RolesController _controller;

        [TestInitialize]
        public void Setup()
        {
            _roleManagerMock = new Mock<RoleManager<IdentityRole>>(
                Mock.Of<IRoleStore<IdentityRole>>(),
                null,
                null,
                null,
                null);

            _userManagerMock = new Mock<UserManager<IdentityUser>>(
                Mock.Of<IUserStore<IdentityUser>>(),
                null,
                new PasswordHasher<IdentityUser>(),
                null,
                null,
                null,
                null,
                null,
                null);

            _roleManagerMock
                .Setup(x => x.FindByIdAsync(RoleId))
                .ReturnsAsync(new IdentityRole("Admin") { Id = RoleId });
            _userManagerMock
                .Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
                .Returns(CurrentUserId);

            _controller = new RolesController(
                _roleManagerMock.Object,
                _userManagerMock.Object,
                Mock.Of<ILogger<RolesController>>())
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(
                            [new Claim(ClaimTypes.NameIdentifier, CurrentUserId)], "test"))
                    }
                }
            };
        }

        [TestMethod]
        public async Task AddOrRemoveRoleUser_AdminCannotGrantAdminToSelf()
        {
            _userManagerMock
                .Setup(x => x.FindByIdAsync(CurrentUserId))
                .ReturnsAsync(new IdentityUser { Id = CurrentUserId });

            var result = await _controller.AddOrRemoveRoleUser(
                RoleId,
                CurrentUserId,
                new AddRemoveRoleRequest { RoleId = RoleId, UserId = CurrentUserId, Add = true });

            Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
            _userManagerMock.Verify(
                x => x.AddToRoleAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()),
                Times.Never);
        }

        [TestMethod]
        public async Task AddOrRemoveRoleUser_AdminCanGrantAdminToAnotherUser()
        {
            var otherUserId = "admin-2";

            _userManagerMock
                .Setup(x => x.FindByIdAsync(otherUserId))
                .ReturnsAsync(new IdentityUser { Id = otherUserId });
            _userManagerMock
                .Setup(x => x.AddToRoleAsync(It.IsAny<IdentityUser>(), "Admin"))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock
                .Setup(x => x.UpdateSecurityStampAsync(It.IsAny<IdentityUser>()))
                .ReturnsAsync(IdentityResult.Success);

            var result = await _controller.AddOrRemoveRoleUser(
                RoleId,
                otherUserId,
                new AddRemoveRoleRequest { RoleId = RoleId, UserId = otherUserId, Add = true });

            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            _userManagerMock.Verify(
                x => x.AddToRoleAsync(It.Is<IdentityUser>(u => u.Id == otherUserId), "Admin"),
                Times.Once);
            _userManagerMock.Verify(
                x => x.UpdateSecurityStampAsync(It.Is<IdentityUser>(u => u.Id == otherUserId)),
                Times.Once);
        }
    }
}
