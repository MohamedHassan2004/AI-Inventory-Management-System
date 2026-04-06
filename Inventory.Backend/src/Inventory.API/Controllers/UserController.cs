using Inventory.Application.DTOs.Auth;
using Inventory.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Inventory.API.Controllers
{
    [Route("api/users")]
    [Authorize]
    public class UserController : ApiBaseController
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [Authorize(Policy = "PendingIdentityUpload, Active")]
        [HttpPost("identity-image")]
        public async Task<IActionResult> UploadIdentityImage([FromForm] UploadIdentityImgDto uploadIdentity)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _userService.UploadUserIdentityAsync(userId, uploadIdentity);
            return HandleResult(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{userId}/status")]
        public async Task<IActionResult> GetUserStatus(string userId)
        {
            var result = await _userService.GetUserStatusAsync(userId);
            return HandleResult(result);
        }
    }
}
