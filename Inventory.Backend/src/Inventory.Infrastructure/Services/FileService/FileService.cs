using Inventory.Application.Interfaces;
using Inventory.Domain.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Inventory.Infrastructure.Services.FileService;

public class FileService : IFileService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FileService> _logger;
    private readonly FileServiceOptions _options;
    private readonly ILocalizationService _localizationService;

    public FileService(
        IWebHostEnvironment env,
        ILogger<FileService> logger,
        IOptions<FileServiceOptions> options,
        ILocalizationService localizationService)
    {
        _env = env;
        _logger = logger;
        _options = options.Value;
        _localizationService = localizationService;
    }

    public async Task<Result<string>> SaveFileAsync(IFormFile file, string folderPath)
    {
        try
        {
            
            var maxSizeInBytes = _options.MaxFileSizeInMB * 1024 * 1024;
            if (file.Length > maxSizeInBytes)
            {
                return Result.Failure<string>(new Error(
                    "FILE_TOO_LARGE",
                    _localizationService.GetMessage("FileTooLarge", _options.MaxFileSizeInMB),
                    ErrorType.Validation));
            }

            
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_options.AllowedExtensions.Contains(extension))
            {
                return Result.Failure<string>(new Error(
                    "INVALID_FILE_TYPE",
                    _localizationService.GetMessage("InvalidFileType", extension, string.Join(", ", _options.AllowedExtensions)),
                    ErrorType.Validation));
            }

            
            if (!_options.AllowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                return Result.Failure<string>(new Error(
                    "INVALID_MIME_TYPE",
                    _localizationService.GetMessage("InvalidMimeType", file.ContentType),
                    ErrorType.Validation));
            }

            
            var sanitizedFolderPath = SanitizePath(folderPath);
            if (sanitizedFolderPath == null)
            {
                return Result.Failure<string>(new Error(
                    "INVALID_PATH",
                    _localizationService.GetMessage("InvalidFolderPath"),
                    ErrorType.Validation));
            }

            
            var uploadsFolder = Path.Combine(_env.WebRootPath, _options.UploadFolder, sanitizedFolderPath);
            Directory.CreateDirectory(uploadsFolder);

            
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            
            await using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            _logger.LogInformation("File uploaded successfully: {Path}", filePath);

            
            var relativePath = Path.Combine(_options.UploadFolder, sanitizedFolderPath, fileName)
                .Replace(Path.DirectorySeparatorChar, '/');

            return Result.Success<string>(relativePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during file save operation");
            return Result.Failure<string>(new Error("FILE_SAVE_FAILED", _localizationService.GetMessage("FileSaveFailed"), ErrorType.Failure));
        }
    }

    public async Task<Result> DeleteFileAsync(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return Result.Failure(new Error("INVALID_PATH", _localizationService.GetMessage("PathCannotBeEmpty"), ErrorType.Validation));
        }

        var fullPath = GetAbsolutePath(relativePath);

        
        var uploadsRoot = Path.Combine(_env.WebRootPath, _options.UploadFolder);
        if (!fullPath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Attempted to delete file outside uploads directory: {Path}", fullPath);
            return Result.Failure(new Error("INVALID_PATH", _localizationService.GetMessage("InvalidPath"), ErrorType.Validation));
        }

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("File not found: {Path}", fullPath);
            return Result.Failure(new Error("NOT_FOUND", _localizationService.GetMessage("FileNotFound"), ErrorType.NotFound));
        }

        try
        {
            await Task.Run(() => File.Delete(fullPath));
            _logger.LogInformation("File deleted successfully: {Path}", fullPath);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during file deletion: {Path}", fullPath);
            return Result.Failure(new Error("FILE_DELETE_FAILED", _localizationService.GetMessage("FileDeleteFailed"), ErrorType.Failure));
        }
    }

    public string GetAbsolutePath(string relativePath)
    {
        var normalizedPath = relativePath.Replace('/', Path.DirectorySeparatorChar)
                                         .Replace('\\', Path.DirectorySeparatorChar);
        return Path.Combine(_env.WebRootPath, normalizedPath);
    }

    private static string? SanitizePath(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return string.Empty;
        }

        var sanitized = folderPath.Replace("..", string.Empty)
                                  .Replace("~", string.Empty)
                                  .Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, '/', '\\');

        if (Path.IsPathRooted(sanitized))
        {
            return null;
        }

        return sanitized;
    }
}
