using Inventory.Application.DTOs.Auth;
using Inventory.Application.Interfaces;
using Inventory.Application.Interfaces.Auth;
using Inventory.Domain.Entities.Users;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Shared;
using Inventory.Infrastructure.Data;
using Inventory.Infrastructure.Data.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Inventory.Infrastructure.Services.Auth;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuthService> _logger;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly JwtSettings _jwtSettings;
    private readonly ILocalizationService _localizationService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext context,
        ILogger<AuthService> logger,
        IDateTimeProvider dateTimeProvider,
        IOptions<JwtSettings> jwtSettings,
        ILocalizationService localizationService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
        _jwtSettings = jwtSettings.Value;
        _localizationService = localizationService;
    }

    public async Task<Result<TokenDto>> LoginAsync(LoginDto loginDto)
    {
        _logger.LogInformation("Login attempt for user: {UserName}", loginDto.UserName);

        var user = await _userManager.FindByNameAsync(loginDto.UserName);
        if (user is null)
        {
            _logger.LogWarning("Login failed: User '{UserName}' not found", loginDto.UserName);
            return Result.Failure<TokenDto>(new Error("INVALID_CREDENTIAL", _localizationService.GetMessage("InvalidCredentials"), ErrorType.Unauthorized));
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            _logger.LogWarning("Login failed: User '{UserName}' is locked out", user.UserName);
            return Result.Failure<TokenDto>(new Error("LOCKED_OUT", _localizationService.GetMessage("UserLockedOut"), ErrorType.Unauthorized));
        }

        var checkPasswordResult = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, true);
        if (!checkPasswordResult.Succeeded)
        {
            await _userManager.AccessFailedAsync(user);
            var failedAttempts = await _userManager.GetAccessFailedCountAsync(user);
            _logger.LogWarning("Login failed: Invalid password for user '{UserName}'. Attempts: {Attempts}",
                user.UserName, failedAttempts);
            return Result.Failure<TokenDto>(new Error("INVALID_CREDENTIAL", _localizationService.GetMessage("InvalidCredentials"), ErrorType.Unauthorized));
        }

        try
        {
            user.Login(_dateTimeProvider);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Login failed: User '{UserName}' cannot login - {Reason}", user.UserName, ex.Message);
            return Result.Failure<TokenDto>(new Error("INVALID_OPERATION", ex.Message, ErrorType.Validation));
        }

        var claims = await GenerateUserClaimsAsync(user);
        var accessToken = GenerateAccessToken(claims);
        var refreshTokenString = GenerateRefreshToken();

        
        var existingTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == user.Id && !rt.IsRevoked)
            .ToListAsync();

        foreach (var existingToken in existingTokens)
        {
            existingToken.RevokeRefreshToken();
        }

        
        var newRefreshToken = new RefreshToken
        {
            Token = refreshTokenString,
            UserId = user.Id,
            ExpiryDate = _dateTimeProvider.UtcNow.AddDays(_jwtSettings.RefreshTokenDurationInDays),
            IsRevoked = false
        };

        await _context.RefreshTokens.AddAsync(newRefreshToken);
        await _userManager.UpdateAsync(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User '{UserName}' logged in successfully", user.UserName);

        return Result.Success(new TokenDto(accessToken, refreshTokenString));
    }

    public async Task LogoutAsync(string userId)
    {
        _logger.LogInformation("Logout initiated for user ID: {UserId}", userId);

        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.IsRevoked == false)
            .ToListAsync();

        if (activeTokens.Any())
        {
            foreach (var token in activeTokens)
            {
                token.RevokeRefreshToken();
            }

            _context.RefreshTokens.UpdateRange(activeTokens);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Refresh tokens revoked for user ID: {UserId}", userId);
        }

        await _signInManager.SignOutAsync();
    }

    public async Task<Result> RegisterAsync(RegisterDto registerDto)
    {
        _logger.LogInformation("Registration attempt - UserName: {UserName}, Email: {Email}",
            registerDto.UserName, registerDto.Email);

        var userNameExist = await _userManager.FindByNameAsync(registerDto.UserName);
        if (userNameExist != null)
        {
            _logger.LogWarning("Registration failed: Username '{UserName}' already exists", registerDto.UserName);
            return Result.Failure(new Error("USERNAME_ALREADY_EXIST", _localizationService.GetMessage("UsernameAlreadyExists"), ErrorType.Conflict));
        }

        var emailExist = await _userManager.FindByEmailAsync(registerDto.Email);
        if (emailExist != null)
        {
            _logger.LogWarning("Registration failed: Email '{Email}' already exists", registerDto.Email);
            return Result.Failure(new Error("EMAIL_ALREADY_EXIST", _localizationService.GetMessage("EmailAlreadyExists"), ErrorType.Conflict));
        }

        try
        {
            var user = new ApplicationUser(
                registerDto.UserName,
                registerDto.FullName,
                registerDto.Email,
                registerDto.PhoneNumber);

            var createResult = await _userManager.CreateAsync(user, "Welcome123@");
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                _logger.LogError("Registration failed: Could not create user '{UserName}'. Errors: {Errors}",
                    registerDto.UserName, errors);
                return Result.Failure(new Error("UNEXPECTED_ERROR", errors, ErrorType.Failure));
            }

            foreach (var role in registerDto.Roles)
            {
                await _userManager.AddToRoleAsync(user, role.ToString());
            }

            _logger.LogInformation("User '{UserName}' (ID: {UserId}) registered successfully with roles: {Roles}",
                user.UserName, user.Id, string.Join(", ", registerDto.Roles));

            return Result.Success();
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Registration failed: Invalid data for user '{UserName}'", registerDto.UserName);
            return Result.Failure(new Error("INVALID_DATA", ex.Message, ErrorType.Validation));
        }
    }

    public async Task<Result> ChangePasswordAsync(string userId, ChangePasswordDto changePasswordDto)
    {
        _logger.LogInformation("Password change request for user ID: {UserId}", userId);

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            _logger.LogWarning("Password change failed: User with ID {UserId} not found", userId);
            return Result.Failure(new Error("NOT_FOUND", _localizationService.GetMessage("UserNotFound"), ErrorType.NotFound));
        }

        var changeResult = await _userManager.ChangePasswordAsync(
            user,
            changePasswordDto.CurrentPassword,
            changePasswordDto.NewPassword);

        if (!changeResult.Succeeded)
        {
            var errors = string.Join(", ", changeResult.Errors.Select(e => e.Description));
            _logger.LogWarning("Password change failed for user '{UserName}': {Errors}", user.UserName, errors);
            return Result.Failure(new Error("PASSWORD_CHANGE_FAILED", errors, ErrorType.Failure));
        }

        user.PasswordChanged();
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Password changed successfully for user '{UserName}'", user.UserName);

        return Result.Success();
    }

    public async Task<Result<TokenDto>> RefreshTokenAsync(string refreshToken)
    {
        _logger.LogDebug("Token refresh requested using refresh token");

        
        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (storedToken is null)
        {
            _logger.LogWarning("Token refresh failed: Refresh token not found");
            return Result.Failure<TokenDto>(new Error("INVALID_REFRESH_TOKEN", _localizationService.GetMessage("InvalidRefreshToken"), ErrorType.Unauthorized));
        }

        var user = storedToken.User;
        if (user is null)
        {
            _logger.LogWarning("Token refresh failed: User not found for refresh token ID {RefreshTokenId}", storedToken.Id);
            return Result.Failure<TokenDto>(new Error("NOT_FOUND", _localizationService.GetMessage("UserNotFound"), ErrorType.NotFound));
        }

        if (storedToken.IsRevoked)
        {
            _logger.LogWarning("Token refresh failed: Refresh token is revoked for user '{UserName}'", user.UserName);
            return Result.Failure<TokenDto>(new Error("INVALID_REFRESH_TOKEN", _localizationService.GetMessage("RefreshTokenRevoked"), ErrorType.Unauthorized));
        }

        if (storedToken.ExpiryDate <= _dateTimeProvider.UtcNow)
        {
            _logger.LogWarning("Token refresh failed: Refresh token expired for user '{UserName}'", user.UserName);
            return Result.Failure<TokenDto>(new Error("INVALID_REFRESH_TOKEN", _localizationService.GetMessage("RefreshTokenExpired"), ErrorType.Unauthorized));
        }

        
        storedToken.RevokeRefreshToken();

        
        var claims = await GenerateUserClaimsAsync(user);
        var newAccessToken = GenerateAccessToken(claims);
        var newRefreshTokenString = GenerateRefreshToken();

        
        var newRefreshTokenEntity = new RefreshToken
        {
            Token = newRefreshTokenString,
            UserId = user.Id,
            ExpiryDate = _dateTimeProvider.UtcNow.AddDays(_jwtSettings.RefreshTokenDurationInDays),
            IsRevoked = false
        };

        await _context.RefreshTokens.AddAsync(newRefreshTokenEntity);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Token refreshed successfully for user '{UserName}'", user.UserName);

        return Result.Success(new TokenDto(newAccessToken, newRefreshTokenString));
    }

    public async Task<bool> IsUserNameExist(string userName)
    {
        var exists = await _userManager.FindByNameAsync(userName) is not null;
        _logger.LogDebug("Username check: '{UserName}' exists = {Exists}", userName, exists);
        return exists;
    }

    public async Task<bool> IsEmailExist(string email)
    {
        var exists = await _userManager.FindByEmailAsync(email) is not null;
        _logger.LogDebug("Email check: '{Email}' exists = {Exists}", email, exists);
        return exists;
    }

    public async Task<bool> IsPhoneNumberExist(string phoneNumber)
    {
        var exists = await _userManager.Users.AnyAsync(u => u.PhoneNumber == phoneNumber);
        _logger.LogDebug("Phone number check: '{PhoneNumber}' exists = {Exists}", phoneNumber, exists);
        return exists;
    }

    #region Private Helpers

    private async Task<List<Claim>> GenerateUserClaimsAsync(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.Email, user.Email!),
        };


        var userRoles = await _userManager.GetRolesAsync(user);
        foreach (var role in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        return claims;
    }

    private string GenerateAccessToken(IList<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry = _dateTimeProvider.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiry,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    #endregion
}
