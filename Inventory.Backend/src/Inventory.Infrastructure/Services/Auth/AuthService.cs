using Inventory.Application.DTOs.Auth;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities.Users;
using Inventory.Domain.Enums;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Inventory.Infrastructure.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AuthService> _logger;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IConfiguration _config;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AuthService> logger,
            IDateTimeProvider dateTimeProvider,
            IConfiguration config
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _dateTimeProvider = dateTimeProvider;
            _config = config;
        }

        public async Task<Result<TokenDto>> LoginAsync(LoginDto loginDto)
        {
            var user = await _userManager.FindByNameAsync(loginDto.UserName);
            if(user is null)
            {
                return Result.Failure<TokenDto>("NOT_FOUND", "User not found");
            }

            if (await _userManager.IsLockedOutAsync(user))
                return Result.Failure<TokenDto>("LOCKED_OUT", "User is locked out.");

            var checkPasswordResult = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, true);
            if (!checkPasswordResult.Succeeded)
            {
                await _userManager.AccessFailedAsync(user);
                return Result.Failure<TokenDto>("INVAILD_CREDENTIAL" ,"Invalid Username or password.");
            }

            try
            {
                user.Login(_dateTimeProvider);
            }
            catch(InvalidOperationException e)
            {
                _logger.LogWarning(e.Message);
                return Result.Failure<TokenDto>("INVALID_OPERATION", e.Message);
            }

            var claims = GenerateUserClaims(user);
            var accessToken = GenerateAccessToken(claims);
            var refreshToken = GenerateRefreshToken();

            user.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));
            await _userManager.UpdateAsync(user);

            var token = new TokenDto(accessToken, refreshToken);

            return Result.Success<TokenDto>(token, "User signed in successfully!");
        }
   
        public async Task LogoutAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user != null)
            {
                user.RevokeRefreshToken();
                await _userManager.UpdateAsync(user);
            }

            await _signInManager.SignOutAsync();
        }

        public async Task<Result> RegisterAsync(RegisterDto registerDto)
        {
            var userNameExist = await _userManager.FindByNameAsync(registerDto.UserName);
            if (userNameExist != null) 
            {
                Result.Failure("USERNAME_ALREADY_EXIST", "Username is already exist.");
            }

            var emailExist = await _userManager.FindByEmailAsync(registerDto.Email);
            if (emailExist != null)
            {
                Result.Failure("EMAIL_ALREADY_EXIST", "Email is already exist.");
            }

            try
            {
                var user = new ApplicationUser
                    (registerDto.UserName, registerDto.FullName, registerDto.Email, registerDto.PhoneNumber, registerDto.Role);

                var createUserResult = await _userManager.CreateAsync(user, "Welcome123@");
                if (!createUserResult.Succeeded)
                {
                    var errors = string.Join(", ", createUserResult.Errors.Select(e => e.Description));
                    return Result.Failure("UNEXPECTED_ERROR", errors);
                }

                await _userManager.AddToRoleAsync(user, registerDto.Role.ToString());
            }
            catch (ArgumentException e)
            {
                _logger.LogError(e.Message);
                Result.Failure("INVALID_DATA", e.Message);
            }

            return Result.Success("user registered successfully.");
        }

        public async Task<Result<TokenDto>> RefreshTokenAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if(user is null)
            {
                return Result.Failure<TokenDto>("NOT_FOUND", "User not found");
            }

            if (user.RefreshToken == null)
            {
                return Result.Failure<TokenDto>("INVALID_REFRESH_TOKEN" ,"Invalid or expired refresh token.");
            }

            user.RevokeRefreshToken();

            var claims = GenerateUserClaims(user);
            var newAccessToken = GenerateAccessToken(claims);
            var newRefreshToken = GenerateRefreshToken();

            user.SetRefreshToken(newRefreshToken, DateTime.UtcNow.AddDays(7));
            await _userManager.UpdateAsync(user);

            var tokenDto = new TokenDto(newAccessToken, newRefreshToken);

            return Result.Success<TokenDto>(tokenDto, "Token Refreshed successfully");
        }

        public async Task<Result> DeleteUserAsync(string userId)
        {
            var user = _userManager.Users.FirstOrDefault(u => u.Id == userId);
            if(user is null)
            {
                return Result.Failure("NOT_FOUND", "User not found");
            }

            user.MarkAsDeleted(_dateTimeProvider);
            await _userManager.UpdateAsync(user);
            return Result.Success("User deleted successfully");
        }

        public async Task<Result> RestoreUserAsync(string userId)
        {
            var user = _userManager.Users.IgnoreQueryFilters().FirstOrDefault(u => u.Id == userId);
            if (user is null)
            {
                return Result.Failure("NOT_FOUND", "User not found");
            }
            user.Restore();
            await _userManager.UpdateAsync(user);
            return Result.Success("User restored successfully");
        }

        public async Task<Result> ChangeUserRoleAsync(string userId, UserRole newRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return Result.Failure("NOT_FOUND", "User not found");
            }

            try
            {
                user.ChangeRole(newRole);
            }
            catch (InvalidOperationException e)
            {
                _logger.LogWarning(e.Message);
                return Result.Failure("INVALID_OPERATION", e.Message);
            }
            catch (ArgumentException e)
            {
                _logger.LogWarning(e.Message);
                return Result.Failure("INVALID_ROLE", e.Message);
            }

            await _userManager.UpdateAsync(user);

            await _userManager.RemoveFromRolesAsync(user, Enum.GetNames<UserRole>());
            await _userManager.AddToRoleAsync(user, newRole.ToString());

            return Result.Success("User role updated successfully");
        }

        public async Task<Result> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) 
            { 
                return Result.Failure("NOT_FOUND", "User not found");
            }
            var changePasswordResult = await _userManager.ChangePasswordAsync(user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                var errors = string.Join(", ", changePasswordResult.Errors.Select(e => e.Description));
                return Result.Failure("PASSWORD_CHANGE_FAILED", errors);
            }

            user.PasswordChanged();
            return Result.Success("Password changed successfully");
        }

        public async Task<bool> IsUserNameExist(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user is not null)
            {
                return false;
            }
            return true;
        }
        public async Task<bool> IsEmailExist(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is not null)
            {
                return false;
            }
            return true;
        }

        #region Helpers
        private List<Claim> GenerateUserClaims(ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            return claims;
        }

        private string GenerateAccessToken(IList<Claim> claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.UtcNow.AddHours(20);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: expiry,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        #endregion
    }
}
