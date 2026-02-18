using Inventory.Application.Interfaces.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Controllers
{
    [Route("api/roles")]
    [Authorize(Roles = "Admin")]
    public class RoleController : ApiBaseController
    {
        private readonly IRoleService _roleService;

        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserRoles(string userId)
        {
            var result = await _roleService.GetUserRolesAsync(userId);
            return HandleResult(result);
        }

        [HttpPost("{userId}")]
        public async Task<IActionResult> AddUserRole(string userId, [FromQuery] string role)
        {
            var result = await _roleService.AddUserRoleAsync(userId, role);
            return HandleResult(result);
        }

        [HttpDelete("{userId}")]
        public async Task<IActionResult> RemoveUserRole(string userId, [FromQuery] string role)
        {
            var result = await _roleService.RemoveUserRoleAsync(userId, role);
            return HandleResult(result);
        }
    }
}
