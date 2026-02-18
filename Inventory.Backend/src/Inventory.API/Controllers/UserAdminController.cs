using Inventory.Application.Interfaces.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Controllers
{
    [Route("api/admin/users")]
    [Authorize(Roles = "Admin")]
    public class UserAdminController : ApiBaseController
    {
        private readonly IUserAdminService _userAdminService;

        public UserAdminController(IUserAdminService userAdminService)
        {
            _userAdminService = userAdminService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAccounts()
        {
            var result = await _userAdminService.GetAllAccountsAsync();
            return HandleResult(result);
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingAccounts()
        {
            var result = await _userAdminService.GetPendingAccountsAsync();
            return HandleResult(result);
        }

        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var result = await _userAdminService.DeleteUserAsync(userId);
            return HandleResult(result);
        }

        [HttpPut("{userId}/restore")]
        public async Task<IActionResult> RestoreUser(string userId)
        {
            var result = await _userAdminService.RestoreUserAsync(userId);
            return HandleResult(result);
        }

        [HttpPut("{userId}/approve")]
        public async Task<IActionResult> ApproveAccount(string userId)
        {
            var result = await _userAdminService.ApproveAccountAsync(userId);
            return HandleResult(result);
        }

        [HttpPut("{userId}/reject")]
        public async Task<IActionResult> RejectAccount(string userId, [FromQuery] string reason)
        {
            var result = await _userAdminService.RejectAccountAsync(userId, reason);
            return HandleResult(result);
        }
    }
}
