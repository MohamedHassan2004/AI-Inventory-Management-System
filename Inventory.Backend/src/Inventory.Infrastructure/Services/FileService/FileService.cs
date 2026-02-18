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
            // Validate file size
            var maxSizeInBytes = _options.MaxFileSizeInMB * 1024 * 1024;
            if (file.Length > maxSizeInBytes)
            {
                return Result.Failure<string>(
                    "FILE_TOO_LARGE",
                    _localizationService.GetMessage("FileTooLarge", _options.MaxFileSizeInMB));
            }

            // Validate file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_options.AllowedExtensions.Contains(extension))
            {
                return Result.Failure<string>(
                    "INVALID_FILE_TYPE",
                    _localizationService.GetMessage("InvalidFileType", extension, string.Join(", ", _options.AllowedExtensions)));
            }

            // Validate MIME type
            if (!_options.AllowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                return Result.Failure<string>(
                    "INVALID_MIME_TYPE",
                    _localizationService.GetMessage("InvalidMimeType", file.ContentType));
            }

            // Sanitize and validate folder path to prevent path traversal
            var sanitizedFolderPath = SanitizePath(folderPath);
            if (sanitizedFolderPath == null)
            {
                return Result.Failure<string>(
                    "INVALID_PATH",
                    _localizationService.GetMessage("InvalidFolderPath"));
            }

            // Create upload directory
            var uploadsFolder = Path.Combine(_env.WebRootPath, _options.UploadFolder, sanitizedFolderPath);
            Directory.CreateDirectory(uploadsFolder);

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            // Save file
            await using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            _logger.LogInformation("File uploaded successfully: {Path}", filePath);

            // Return relative path with forward slashes for web compatibility
            var relativePath = Path.Combine(_options.UploadFolder, sanitizedFolderPath, fileName)
                .Replace(Path.DirectorySeparatorChar, '/');

            return Result.Success<string>(relativePath, _localizationService.GetMessage("FileSavedSuccess"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during file save operation");
            return Result.Failure<string>("FILE_SAVE_FAILED", _localizationService.GetMessage("FileSaveFailed"));
        }
    }

    public async Task<Result> DeleteFileAsync(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return Result.Failure("INVALID_PATH", _localizationService.GetMessage("PathCannotBeEmpty"));
        }

        var fullPath = GetAbsolutePath(relativePath);

        // Validate path is within allowed directory
        var uploadsRoot = Path.Combine(_env.WebRootPath, _options.UploadFolder);
        if (!fullPath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Attempted to delete file outside uploads directory: {Path}", fullPath);
            return Result.Failure("INVALID_PATH", _localizationService.GetMessage("InvalidPath"));
        }

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("File not found: {Path}", fullPath);
            return Result.Failure("NOT_FOUND", _localizationService.GetMessage("FileNotFound"));
        }

        try
        {
            await Task.Run(() => File.Delete(fullPath));
            _logger.LogInformation("File deleted successfully: {Path}", fullPath);
            return Result.Success(_localizationService.GetMessage("FileDeletedSuccess"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during file deletion: {Path}", fullPath);
            return Result.Failure("FILE_DELETE_FAILED", _localizationService.GetMessage("FileDeleteFailed"));
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
