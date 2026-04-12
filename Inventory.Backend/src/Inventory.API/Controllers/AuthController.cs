using Inventory.Application.DTOs.Auth;
using Inventory.Application.Interfaces;
using Inventory.Application.Interfaces.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Inventory.API.Controllers
{
    [Route("api/auth")]
    public class AuthController : ApiBaseController
    {
        private readonly IAuthService _authService;
        private readonly ILocalizationService _localizationService;

        public AuthController(IAuthService authService, ILocalizationService localizationService)
        {
            _authService = authService;
            _localizationService = localizationService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var result = await _authService.LoginAsync(loginDto);
            return HandleResult(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            var result = await _authService.RegisterAsync(registerDto);
            return HandleResult(result);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await _authService.LogoutAsync(userId);
            return Ok(new { Message = _localizationService.GetMessage("LogoutSuccess") });
        }

        [Authorize]
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _authService.RefreshTokenAsync(userId, dto.RefreshToken);
            return HandleResult(result);
        }

        [Authorize]
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _authService.ChangePasswordAsync(userId, changePasswordDto);
            return HandleResult(result);
        }

        [AllowAnonymous]
        [HttpGet("is-username-exist")]
        public async Task<IActionResult> IsUserNameExist([FromQuery] string userName)
        {
            var exists = await _authService.IsUserNameExist(userName);
            return Ok(new { exists });
        }

        [AllowAnonymous]
        [HttpGet("is-email-exist")]
        public async Task<IActionResult> IsEmailExist([FromQuery] string email)
        {
            var exists = await _authService.IsEmailExist(email);
            return Ok(new { exists });
        }

        [AllowAnonymous]
        [HttpGet("is-phone-number-exist")]
        public async Task<IActionResult> IsPhoneNumberExist([FromQuery] string phoneNumber)
        {
            var exists = await _authService.IsPhoneNumberExist(phoneNumber);
            return Ok(new { exists });
        }
    }
}
