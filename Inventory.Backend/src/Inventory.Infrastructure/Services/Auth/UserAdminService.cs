using Inventory.Application.DTOs;
using Inventory.Application.Interfaces.Auth;
using Inventory.Domain.Entities.Users;
using Inventory.Domain.Enums;
using Inventory.Domain.Interfaces;
using Inventory.Domain.Shared;
using Inventory.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.Services.Auth;

public class UserAdminService : IUserAdminService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<UserAdminService> _logger;

    public UserAdminService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IDateTimeProvider dateTimeProvider,
        ILogger<UserAdminService> logger)
    {
        _context = context;
        _userManager = userManager;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result> DeleteUserAsync(string userId)
    {
        _logger.LogInformation("Delete user request for user ID: {UserId}", userId);

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            _logger.LogWarning("Delete user failed: User with ID {UserId} not found", userId);
            return Result.Failure("NOT_FOUND", "User not found");
        }

        user.MarkAsDeleted(_dateTimeProvider);
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("User '{UserName}' (ID: {UserId}) marked as deleted at {DeletedAt}",
            user.UserName, userId, user.DeletedAt);

        return Result.Success("User deleted successfully");
    }

    public async Task<Result> RestoreUserAsync(string userId)
    {
        _logger.LogInformation("Restore user request for user ID: {UserId}", userId);

        var user = await _userManager.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            _logger.LogWarning("Restore user failed: User with ID {UserId} not found", userId);
            return Result.Failure("NOT_FOUND", "User not found");
        }

        if (!user.IsDeleted)
        {
            _logger.LogWarning("Restore user: User '{UserName}' (ID: {UserId}) is not deleted", user.UserName, userId);
            return Result.Failure("INVALID_OPERATION", "User is not deleted");
        }

        user.Restore();
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("User '{UserName}' (ID: {UserId}) restored successfully", user.UserName, userId);

        return Result.Success("User restored successfully");
    }

    public async Task<Result> ApproveAccountAsync(string userId)
    {
        _logger.LogInformation("Account approval request for user ID: {UserId}", userId);

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            _logger.LogWarning("Account approval failed: User with ID {UserId} not found", userId);
            return Result.Failure("NOT_FOUND", "User not found");
        }

        var previousStatus = user.AccountStatus;
        user.ApproveAccount();
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Account approved for user '{UserName}'. Status: {Previous} -> {New}",
            user.UserName, previousStatus, user.AccountStatus);

        return Result.Success("User account approved successfully");
    }

    public async Task<Result> RejectAccountAsync(string userId, string rejectionReason)
    {
        _logger.LogInformation("Account rejection request for user ID: {UserId}", userId);

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            _logger.LogWarning("Account rejection failed: User with ID {UserId} not found", userId);
            return Result.Failure("NOT_FOUND", "User not found");
        }

        var previousStatus = user.AccountStatus;
        user.RejectAccount(rejectionReason);
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Account rejected for user '{UserName}'. Reason: {Reason}",
            user.UserName, rejectionReason);

        return Result.Success("User account rejected successfully");
    }

    public async Task<Result<List<AccountDto>>> GetPendingAccountsAsync()
    {
        _logger.LogDebug("Retrieving pending accounts");

        var accountDtos = await _userManager.Users
            .AsNoTracking()
            .Where(u => u.AccountStatus == AccountStatus.Pending)
            .Select(u => new AccountDto
            {
                UserId = u.Id,
                UserName = u.UserName!,
                IdentityImgUrl = u.IdentityImgUrl,
                CreatedAt = u.CreatedAt,
                AccountStatus = u.AccountStatus,
                Roles = _context.UserRoles
                    .Where(ur => ur.UserId == u.Id)
                    .Join(_context.Roles,
                          ur => ur.RoleId,
                          r => r.Id,
                          (ur, r) => r.Name!)
                    .ToList()
            })
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} pending accounts", accountDtos.Count);

        return Result.Success(accountDtos, "Pending accounts retrieved successfully");
    }

    public async Task<Result<List<AccountDto>>> GetAllAccountsAsync()
    {
        _logger.LogDebug("Retrieving all accounts");

        var accountDtos = await _userManager.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Select(u => new AccountDto
            {
                UserId = u.Id,
                UserName = u.UserName!,
                IdentityImgUrl = u.IdentityImgUrl,
                CreatedAt = u.CreatedAt,
                AccountStatus = u.AccountStatus,
                Roles = _context.UserRoles
                    .Where(ur => ur.UserId == u.Id)
                    .Join(_context.Roles,
                          ur => ur.RoleId,
                          r => r.Id,
                          (ur, r) => r.Name!)
                    .ToList()
            })
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} accounts", accountDtos.Count);

        return Result.Success(accountDtos, "All accounts retrieved successfully");
    }

}
