using Inventory.Application.DTOs.Auth;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities.Users;
using Inventory.Domain.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Inventory.Application.Services;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IFileService _fileService;
    private readonly ILogger<UserService> _logger;
    private readonly ILocalizationService _localizationService;

    public UserService(
        UserManager<ApplicationUser> userManager,
        IFileService fileService,
        ILogger<UserService> logger,
        ILocalizationService localizationService)
    {
        _userManager = userManager;
        _fileService = fileService;
        _logger = logger;
        _localizationService = localizationService;
    }

    public async Task<Result> UploadUserIdentityAsync(string userId, UploadIdentityImgDto uploadIdentity)
    {
        _logger.LogInformation("Upload identity image request for user ID: {UserId}", userId);

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Upload identity failed: User with ID {UserId} not found", userId);
            return Result.Failure(new Error("NOT_FOUND", _localizationService.GetMessage("UserNotFound"), ErrorType.NotFound));
        }

        var saveResult = await _fileService.SaveFileAsync(uploadIdentity.IdentityImageFile, "user-identities");
        if (!saveResult.IsSuccess)
        {
            _logger.LogError("Failed to save identity image for user {UserId}: {Error}", userId, saveResult.Error.Description);
            return Result.Failure(new Error("FILE_UPLOAD_ERROR", _localizationService.GetMessage("IdentityUploadFailed"), ErrorType.Failure));
        }

        user.SetIdentityImgUrl(saveResult.Value);
        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            _logger.LogError("Failed to update user {UserId} with identity image: {Errors}", userId, errors);
            return Result.Failure(new Error("USER_UPDATE_ERROR", _localizationService.GetMessage("IdentityUpdateFailed"), ErrorType.Failure));
        }

        _logger.LogInformation("Identity image uploaded successfully for user '{UserName}'", user.UserName);

        return Result.Success();
    }

    public async Task<Result<string>> GetUserStatusAsync(string userId)
    {
        _logger.LogInformation("Get user status request for user ID: {UserId}", userId);
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Get user status failed: User with ID {UserId} not found", userId);
            return Result.Failure<string>(new Error("NOT_FOUND", _localizationService.GetMessage("UserNotFound"), ErrorType.NotFound));
        }
        var status = user.AccountStatus.ToString();
        _logger.LogInformation("User '{UserName}' status retrieved successfully: {Status}", user.UserName, status);
        return Result.Success(status);
    }

    public async Task<Result<string>> GetIdentityRejectionReasonAsync(string userId)
    {
        _logger.LogInformation("Get user rejection reason request for user ID: {UserId}", userId);
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Get user rejection reason failed: User with ID {UserId} not found", userId);
            return Result.Failure<string>(new Error("NOT_FOUND", _localizationService.GetMessage("UserNotFound"), ErrorType.NotFound));
        }
        var rejectionReason = user.RejectionReason ?? "";
        _logger.LogInformation("User '{UserName}' rejection reason retrieved successfully: {rejectionReason}", user.UserName, rejectionReason);
        return Result.Success(rejectionReason);
    }
}
