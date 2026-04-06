using Inventory.Application.DTOs.Auth;
using Inventory.Domain.Shared;

namespace Inventory.Application.Interfaces;

public interface IUserService
{
    Task<Result> UploadUserIdentityAsync(string userId, UploadIdentityImgDto uploadIdentity);
    Task<Result<string>> GetUserStatusAsync(string userId);
}
