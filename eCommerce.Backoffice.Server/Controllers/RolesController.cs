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
    [IgnoreAntiforgeryToken]
    public class RolesController : ControllerBase
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<IdentityUser> _userManager;

        public RolesController(RoleManager<IdentityRole> roleManager,
            UserManager<IdentityUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

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

            var roleUserResponses = new List<RoleUserResponse>();
            var users = await _userManager.Users.AsNoTracking().ToListAsync();

            foreach (var user in users)
            {
                var roleUserResponse = new RoleUserResponse
                {
                    Id = user.Id,
                    Name = user.UserName,
                    InRole = await _userManager.IsInRoleAsync(user, identityRole.Name)
                };

                roleUserResponses.Add(roleUserResponse);
            }

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
                response.Id = identityRole.Id;
                response.IsSuccess = true;
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

            try
            {
                if (addRemoveRoleRequest.Add)
                {
                    await _userManager.AddToRoleAsync(user, role.Name);
                }
                else
                {
                    await _userManager.RemoveFromRoleAsync(user, role.Name);
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

            await _roleManager.DeleteAsync(role);

            return NoContent();
        }
    }
}