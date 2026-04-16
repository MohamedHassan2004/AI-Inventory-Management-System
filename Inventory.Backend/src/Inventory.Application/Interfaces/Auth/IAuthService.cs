using Inventory.Application.DTOs.Auth;
using Inventory.Domain.Shared;

namespace Inventory.Application.Interfaces.Auth;

public interface IAuthService
{
    Task<Result<TokenDto>> LoginAsync(LoginDto loginDto);
    Task<Result> RegisterAsync(RegisterDto registerDto);
    Task LogoutAsync(string userId);
    Task<Result<TokenDto>> RefreshTokenAsync(string refreshToken);
    Task<Result> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto);
    Task<bool> IsUserNameExist(string userName);
    Task<bool> IsEmailExist(string email);
    Task<bool> IsPhoneNumberExist(string phoneNumber);
}
