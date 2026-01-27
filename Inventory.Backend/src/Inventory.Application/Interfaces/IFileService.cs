using Inventory.Domain.Shared;
using Microsoft.AspNetCore.Http;

namespace Inventory.Application.Interfaces;

public interface IFileService
{
    Task<Result<string>> SaveFileAsync(IFormFile file, string folderPath);
    Task<Result> DeleteFileAsync(string relativePath);
    string GetAbsolutePath(string relativePath);
}