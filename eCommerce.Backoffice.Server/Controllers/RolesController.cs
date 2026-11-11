using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using eCommerce.Backoffice.Shared.Model.Roles;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Backoffice.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public class RolesController(RoleManager<IdentityRole> roleManager,
        UserManager<IdentityUser> userManager) : ControllerBase
    {
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;
        private readonly UserManager<IdentityUser> _userManager = userManager;

        [HttpGet]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetRoles()
        {
            return await _roleManager.Roles.AsNoTracking().Select(x => new RoleDto
            {
                Id = x.Id,
                Name = x.Name
            }).ToListAsync();
        }

        [HttpGet("{id}/users")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<ActionResult<IEnumerable<RoleUserResponse>>> GetRoleUsers(string id)
        {
            var identityRole = await _roleManager.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);

            if (identityRole == null)
            {
                return NotFound();
            }

            // Two queries instead of one IsInRoleAsync round-trip per user (N+1).
            var users = await _userManager.Users.AsNoTracking().ToListAsync();
            var usersInRole = await _userManager.GetUsersInRoleAsync(identityRole.Name);
            var inRoleIds = usersInRole.Select(u => u.Id).ToHashSet(StringComparer.Ordinal);

            var roleUserResponses = users.Select(user => new RoleUserResponse
            {
                Id = user.Id,
                Name = user.UserName,
                InRole = inRoleIds.Contains(user.Id)
            }).ToList();

            return roleUserResponses;
        }

        [HttpPost]
        public async Task<ActionResult<EditRoleResponse>> CreateRole(EditRoleRequest request)
        {
            var response = new EditRoleResponse();
            var identityRole = new IdentityRole { Name = request.Name };
            var result = await _roleManager.CreateAsync(identityRole);

            if (result.Succeeded)
            {
                identityRole = await _roleManager.FindByNameAsync(identityRole.Name);

                if (identityRole == null)
                {
                    response.Errors = ["The role was created but could not be reloaded."];
                }
                else
                {
                    response.Id = identityRole.Id;
                    response.IsSuccess = true;
                }
            }
            else
            {
                response.Errors = result.Errors.Select(x => x.Description);
            }

            return Ok(response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRole(string id, EditRoleRequest request)
        {
            if (id != request.Id)
            {
                return BadRequest();
            }

            var response = new EditRoleResponse();
            var identityRole = await _roleManager.FindByIdAsync(request.Id);

            if (identityRole == null)
            {
                return NotFound();
            }

            if (IsBuiltInRole(identityRole.Name)
                && !string.Equals(identityRole.Name, request.Name, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("The built-in roles cannot be renamed.");
            }

            identityRole.Name = request.Name;

            try
            {
                var result = await _roleManager.UpdateAsync(identityRole);

                if (result.Succeeded)
                {
                    response.Id = identityRole.Id;
                    response.IsSuccess = true;
                }
                else
                {
                    response.Errors = result.Errors.Select(x => x.Description);
                }

                return Ok(response);
            }
            catch (DbUpdateConcurrencyException) when (!_roleManager.Roles.AsNoTracking().Any(r => r.Id == id))
            {
                return NotFound();
            }
        }

        [HttpPut("{roleId}/user/{userId}")]
        public async Task<IActionResult> AddOrRemoveRoleUser(string roleId, string userId, AddRemoveRoleRequest addRemoveRoleRequest)
        {
            if (roleId != addRemoveRoleRequest.RoleId)
            {
                return BadRequest();
            }

            if (userId != addRemoveRoleRequest.UserId)
            {
                return BadRequest();
            }

            var role = await _roleManager.FindByIdAsync(addRemoveRoleRequest.RoleId);

            if (role == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(addRemoveRoleRequest.UserId);

            if (user == null)
            {
                return NotFound();
            }

            if (!addRemoveRoleRequest.Add
                && string.Equals(role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                var administrators = await _userManager.GetUsersInRoleAsync("Admin");

                if (administrators.Count <= 1)
                {
                    return BadRequest("The last administrator role cannot be removed.");
                }
            }

            try
            {
                var roleResult = addRemoveRoleRequest.Add
                    ? await _userManager.AddToRoleAsync(user, role.Name)
                    : await _userManager.RemoveFromRoleAsync(user, role.Name);

                if (!roleResult.Succeeded)
                {
                    // Never report success when Identity rejected the change.
                    return BadRequest(roleResult.Errors.Select(x => x.Description));
                }

                // Role changes must invalidate existing cookies so the new
                // authorization decision takes effect immediately.
                var stampResult = await _userManager.UpdateSecurityStampAsync(user);

                if (!stampResult.Succeeded)
                {
                    return BadRequest(stampResult.Errors.Select(x => x.Description));
                }
            }
            catch (DbUpdateConcurrencyException) when (!_roleManager.Roles.AsNoTracking().Any(r => r.Id == addRemoveRoleRequest.RoleId) || !_userManager.Users.AsNoTracking().Any(u => u.Id == addRemoveRoleRequest.UserId))
            {
                return NotFound();
            }

            return Ok(true);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(string id)
        {
            var role = await _roleManager.Roles.FirstOrDefaultAsync(c => c.Id == id);

            if (role == null)
            {
                return NotFound();
            }

            if (IsBuiltInRole(role.Name))
            {
                return BadRequest("The built-in roles cannot be deleted.");
            }

            try
            {
                await _roleManager.DeleteAsync(role);
            }
            catch (DbUpdateException)
            {
                // Deleting a role that is still assigned to users violates the FK
                // constraint; surface a meaningful 400 instead of a 500.
                return BadRequest("The role could not be deleted because it is still assigned to one or more users.");
            }

            return NoContent();
        }

        private static bool IsBuiltInRole(string roleName) =>
            string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(roleName, "Customer", StringComparison.OrdinalIgnoreCase);
    }
}