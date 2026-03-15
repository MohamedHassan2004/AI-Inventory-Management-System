using Inventory.Application.Interfaces;
using Inventory.Application.Interfaces.Auth;
using Inventory.Domain.Entities.Users;
using Inventory.Domain.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.Services.Auth;

public class RoleService : IRoleService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<RoleService> _logger;
    private readonly ILocalizationService _localizationService;

    public RoleService(
        UserManager<ApplicationUser> userManager,
        ILogger<RoleService> logger,
        ILocalizationService localizationService)
    {
        _userManager = userManager;
        _logger = logger;
        _localizationService = localizationService;
    }

    public async Task<Result> AddUserRoleAsync(string userId, string role)
    {
        _logger.LogInformation("Add role '{Role}' request for user ID: {UserId}", role, userId);

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            _logger.LogWarning("Add role failed: User with ID {UserId} not found", userId);
            return Result.Failure(new Error("NOT_FOUND", _localizationService.GetMessage("UserNotFound"), ErrorType.NotFound));
        }

        if (await _userManager.IsInRoleAsync(user, role))
        {
            _logger.LogWarning("Add role failed: User '{UserName}' already has role '{Role}'", user.UserName, role);
            return Result.Failure(new Error("ALREADY_EXIST", _localizationService.GetMessage("UserAlreadyHasRole"), ErrorType.Conflict));
        }

        var result = await _userManager.AddToRoleAsync(user, role);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Add role failed for user '{UserName}': {Errors}", user.UserName, errors);
            return Result.Failure(new Error("ROLE_ADD_FAILED", errors, ErrorType.Failure));
        }

        _logger.LogInformation("Role '{Role}' added to user '{UserName}' (ID: {UserId})", role, user.UserName, userId);
        return Result.Success();
    }

    public async Task<Result> RemoveUserRoleAsync(string userId, string role)
    {
        _logger.LogInformation("Remove role '{Role}' request for user ID: {UserId}", role, userId);

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            _logger.LogWarning("Remove role failed: User with ID {UserId} not found", userId);
            return Result.Failure(new Error("NOT_FOUND", _localizationService.GetMessage("UserNotFound"), ErrorType.NotFound));
        }

        if (!await _userManager.IsInRoleAsync(user, role))
        {
            _logger.LogWarning("Remove role failed: User '{UserName}' doesn't have role '{Role}'", user.UserName, role);
            return Result.Failure(new Error("NOT_FOUND", _localizationService.GetMessage("UserDoesNotHaveRole"), ErrorType.NotFound));
        }

        var userRoles = await _userManager.GetRolesAsync(user);
        if (userRoles.Count <= 1)
        {
            _logger.LogWarning("Remove role failed: Cannot remove the only role from user '{UserName}'", user.UserName);
            return Result.Failure(new Error("WRONG_OPERATION", _localizationService.GetMessage("CannotRemoveOnlyRole"), ErrorType.Validation));
        }

        var result = await _userManager.RemoveFromRoleAsync(user, role);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Remove role failed for user '{UserName}': {Errors}", user.UserName, errors);
            return Result.Failure(new Error("ROLE_REMOVE_FAILED", errors, ErrorType.Failure));
        }

        _logger.LogInformation("Role '{Role}' removed from user '{UserName}' (ID: {UserId})", role, user.UserName, userId);
        return Result.Success();
    }

    public async Task<Result<List<string>>> GetUserRolesAsync(string userId)
    {
        _logger.LogDebug("Get roles request for user ID: {UserId}", userId);

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            _logger.LogWarning("Get roles failed: User with ID {UserId} not found", userId);
            return Result.Failure<List<string>>(new Error("NOT_FOUND", _localizationService.GetMessage("UserNotFound"), ErrorType.NotFound));
        }

        var roles = await _userManager.GetRolesAsync(user);
        _logger.LogInformation("Retrieved {Count} roles for user '{UserName}'", roles.Count, user.UserName);

        return Result.Success(roles.ToList());
    }
}
