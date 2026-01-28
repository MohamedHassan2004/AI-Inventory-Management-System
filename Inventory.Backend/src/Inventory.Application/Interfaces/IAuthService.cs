using Inventory.Application.DTOs.Auth;
using Inventory.Domain.Enums;
using Inventory.Domain.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.Interfaces
{
    public interface IAuthService
    {
        Task<Result<TokenDto>> LoginAsync(LoginDto loginDto);
        Task<Result> RegisterAsync(RegisterDto registerDto);
        Task LogoutAsync(string userId);
        Task<Result<TokenDto>> RefreshTokenAsync(string userId);
        Task<Result> DeleteUserAsync(string userId);
        Task<Result> RestoreUserAsync(string userId);
        Task<Result> ChangeUserRoleAsync(string userId, UserRole newRole);
        Task<Result> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto);
        Task<bool> IsUserNameExist(string userName);
        Task<bool> IsEmailExist(string email);
    }
}
