using Inventory.Domain.Shared;

namespace Inventory.Application.Interfaces.Auth;

public interface IRoleService
{
    Task<Result> AddUserRoleAsync(string userId, string role);
    Task<Result> RemoveUserRoleAsync(string userId, string role);
    Task<Result<List<string>>> GetUserRolesAsync(string userId);
}
