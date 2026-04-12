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

        [Authorize(Policy = "PendingIdentityUploadOrActive")]
        [HttpPost("identity-image")]
        public async Task<IActionResult> UploadIdentityImage([FromForm] UploadIdentityImgDto uploadIdentity)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _userService.UploadUserIdentityAsync(userId, uploadIdentity);
            return HandleResult(result);
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetUserStatus()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _userService.GetUserStatusAsync(userId);
            return HandleResult(result);
        }

        [HttpGet("rejection-reason")]
        public async Task<IActionResult> GetRejectionReason()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _userService.GetIdentityRejectionReasonAsync(userId);
            return HandleResult(result);
        }
    }
}
