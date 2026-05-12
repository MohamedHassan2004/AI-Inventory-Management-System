using Inventory.Application.DTOs.Auth;
using Inventory.Application.Interfaces;
using Inventory.Application.Interfaces.Auth;
using Inventory.API.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Inventory.Infrastructure.Data.Settings;

namespace Inventory.API.Controllers
{
    [Route("api/auth")]
    public class AuthController : ApiBaseController
    {
        private readonly IAuthService _authService;
        private readonly ILocalizationService _localizationService;
        private readonly RefreshTokenCookieSettings _refreshTokenCookieSettings;
        private readonly int _refreshTokenDurationInDays;

        public AuthController(
            IAuthService authService,
            ILocalizationService localizationService,
            IOptions<RefreshTokenCookieSettings> refreshTokenCookieOptions,
            IOptions<JwtSettings> jwtOptions)
        {
            _authService = authService;
            _localizationService = localizationService;
            _refreshTokenCookieSettings = refreshTokenCookieOptions.Value;
            _refreshTokenDurationInDays = jwtOptions.Value.RefreshTokenDurationInDays;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var result = await _authService.LoginAsync(loginDto);
            if (result.IsSuccess)
            {
                AppendRefreshTokenCookie(result.Value!.RefreshToken);
            }
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
            DeleteRefreshTokenCookie();
            return Ok(new { Message = _localizationService.GetMessage("LogoutSuccess") });
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken()
        {
            if (!Request.Cookies.TryGetValue(_refreshTokenCookieSettings.CookieName, out var refreshToken) ||
                string.IsNullOrWhiteSpace(refreshToken))
            {
                return Unauthorized(new { Message = _localizationService.GetMessage("InvalidRefreshToken") });
            }

            var result = await _authService.RefreshTokenAsync(refreshToken);
            if (result.IsSuccess)
            {
                AppendRefreshTokenCookie(result.Value!.RefreshToken);
            }
            else
            {
                DeleteRefreshTokenCookie();
            }

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

        private void AppendRefreshTokenCookie(string refreshToken)
        {
            Response.Cookies.Append(
                _refreshTokenCookieSettings.CookieName,
                refreshToken,
                CreateRefreshTokenCookieOptions());
        }

        private void DeleteRefreshTokenCookie()
        {
            Response.Cookies.Delete(
                _refreshTokenCookieSettings.CookieName,
                CreateRefreshTokenCookieOptions());
        }

        private CookieOptions CreateRefreshTokenCookieOptions()
        {
            return new CookieOptions
            {
                HttpOnly = _refreshTokenCookieSettings.HttpOnly,
                Secure = _refreshTokenCookieSettings.SecurePolicyAlways,
                SameSite = ParseSameSiteMode(_refreshTokenCookieSettings.SameSite),
                Path = _refreshTokenCookieSettings.Path,
                IsEssential = _refreshTokenCookieSettings.IsEssential,
                Expires = DateTimeOffset.UtcNow.AddDays(_refreshTokenDurationInDays)
            };
        }

        private static SameSiteMode ParseSameSiteMode(string sameSite) =>
            Enum.TryParse<SameSiteMode>(sameSite, true, out var parsedMode)
                ? parsedMode
                : SameSiteMode.Strict;
    }
}
