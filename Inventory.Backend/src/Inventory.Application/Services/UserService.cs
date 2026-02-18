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
            return Result.Failure("NOT_FOUND", _localizationService.GetMessage("UserNotFound"));
        }

        var saveResult = await _fileService.SaveFileAsync(uploadIdentity.IdentityImageFile, "user-identities");
        if (!saveResult.IsSuccess)
        {
            _logger.LogError("Failed to save identity image for user {UserId}: {Error}", userId, saveResult.Message);
            return Result.Failure("FILE_UPLOAD_ERROR", _localizationService.GetMessage("IdentityUploadFailed"));
        }

        user.SetIdentityImgUrl(saveResult.Value);
        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            _logger.LogError("Failed to update user {UserId} with identity image: {Errors}", userId, errors);
            return Result.Failure("USER_UPDATE_ERROR", _localizationService.GetMessage("IdentityUpdateFailed"));
        }

        _logger.LogInformation("Identity image uploaded successfully for user '{UserName}'", user.UserName);

        return Result.Success(_localizationService.GetMessage("IdentityUploadedSuccess"));
    }

    
}
