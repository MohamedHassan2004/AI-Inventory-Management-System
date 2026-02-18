using Inventory.Application.DTOs;
using Inventory.Domain.Shared;

namespace Inventory.Application.Interfaces.Auth;

public interface IUserAdminService
{
    Task<Result> DeleteUserAsync(string userId);
    Task<Result> RestoreUserAsync(string userId);
    Task<Result> ApproveAccountAsync(string userId);
    Task<Result> RejectAccountAsync(string userId, string rejectionReason);
    Task<Result<List<AccountDto>>> GetPendingAccountsAsync();
    Task<Result<List<AccountDto>>> GetAllAccountsAsync();
}
