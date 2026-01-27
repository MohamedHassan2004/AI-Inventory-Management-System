using FluentAssertions;
using Inventory.Infrastructure.Services.FileService;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Inventory.Infrastructure.Test.Services;

public class FileServiceTests : IDisposable
{
    #region Test Setup and Dependencies

    // Mock dependencies for the FileService
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly Mock<ILogger<FileService>> _mockLogger;
    private readonly Mock<IOptions<FileServiceOptions>> _mockOptions;

    private readonly FileService _fileService;
    private readonly FileServiceOptions _testOptions;
    private readonly string _testWebRootPath;

    public FileServiceTests()
    {
        _testWebRootPath = Path.Combine(Path.GetTempPath(), $"FileServiceTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testWebRootPath);

        _mockEnvironment = new Mock<IWebHostEnvironment>();
        _mockEnvironment.Setup(e => e.WebRootPath).Returns(_testWebRootPath);
        _mockLogger = new Mock<ILogger<FileService>>();
        _testOptions = new FileServiceOptions
        {
            MaxFileSizeInMB = 5,  // 5MB limit for testing
            UploadFolder = "uploads",
            AllowedExtensions = new List<string> { ".jpg", ".jpeg", ".png", ".pdf" },
            AllowedMimeTypes = new List<string> { "image/jpeg", "image/png", "application/pdf" }
        };
        _mockOptions = new Mock<IOptions<FileServiceOptions>>();
        _mockOptions.Setup(o => o.Value).Returns(_testOptions);

        _fileService = new FileService(
            _mockEnvironment.Object,
            _mockLogger.Object,
            _mockOptions.Object);
    }

    public void Dispose()
    {
        // Clean up the temporary directory after tests
        if (Directory.Exists(_testWebRootPath))
        {
            Directory.Delete(_testWebRootPath, recursive: true);
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a mock IFormFile for testing file upload scenarios.
    /// IFormFile represents an uploaded file in ASP.NET Core.
    /// </summary>
    /// <param name="fileName">The name of the file including extension</param>
    /// <param name="content">The file content as a string (will be converted to bytes)</param>
    /// <param name="contentType">The MIME type of the file (e.g., "image/jpeg")</param>
    /// <returns>A mocked IFormFile instance</returns>
    private static Mock<IFormFile> CreateMockFormFile(
        string fileName = "test.jpg",
        string content = "test content",
        string contentType = "image/jpeg")
    {
        // Convert string content to a memory stream
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

        var mockFile = new Mock<IFormFile>();

        // Setup file properties
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.Length).Returns(stream.Length);
        mockFile.Setup(f => f.ContentType).Returns(contentType);

        // Setup CopyToAsync to actually write content to the target stream
        // This simulates real file upload behavior
        mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns<Stream, CancellationToken>((targetStream, _) =>
            {
                stream.Position = 0;  // Reset stream position before copying
                return stream.CopyToAsync(targetStream);
            });

        return mockFile;
    }

    /// <summary>
    /// Creates a mock IFormFile with a specific size for testing file size validation.
    /// </summary>
    /// <param name="sizeInBytes">The desired file size in bytes</param>
    /// <param name="fileName">The name of the file</param>
    /// <param name="contentType">The MIME type</param>
    /// <returns>A mocked IFormFile with the specified size</returns>
    private static Mock<IFormFile> CreateMockFormFileWithSize(
        long sizeInBytes,
        string fileName = "test.jpg",
        string contentType = "image/jpeg")
    {
        var mockFile = new Mock<IFormFile>();

        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.Length).Returns(sizeInBytes);
        mockFile.Setup(f => f.ContentType).Returns(contentType);

        // For size tests, we don't need actual content copying
        mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return mockFile;
    }

    #endregion

    #region SaveFileAsync Tests - Success Scenarios

    /// <summary>
    /// Test: Verify that a valid file is saved successfully.
    /// This is the happy path test for file upload.
    /// </summary>
    [Fact]
    public async Task SaveFileAsync_WithValidFile_ShouldReturnSuccessWithRelativePath()
    {
        // Arrange: Create a valid mock file
        var mockFile = CreateMockFormFile("test-image.jpg", "fake image content", "image/jpeg");
        var folderPath = "products";

        // Act: Attempt to save the file
        var result = await _fileService.SaveFileAsync(mockFile.Object, folderPath);

        // Assert: Verify the operation succeeded
        result.IsSuccess.Should().BeTrue("file meets all validation criteria");
        result.Value.Should().NotBeNullOrEmpty("a relative path should be returned");
        result.Value.Should().StartWith("uploads/products/", "path should include upload folder and subfolder");
        result.Value.Should().EndWith(".jpg", "file extension should be preserved");
        result.Value.Should().Contain("/", "path should use forward slashes for web compatibility");
    }

    /// <summary>
    /// Test: Verify that the file is physically created on disk.
    /// </summary>
    [Fact]
    public async Task SaveFileAsync_WithValidFile_ShouldCreateFileOnDisk()
    {
        // Arrange
        var mockFile = CreateMockFormFile("photo.png", "PNG image data", "image/png");
        var folderPath = "images";

        // Act
        var result = await _fileService.SaveFileAsync(mockFile.Object, folderPath);

        // Assert: Verify file exists on disk
        result.IsSuccess.Should().BeTrue();

        var absolutePath = _fileService.GetAbsolutePath(result.Value!);
        File.Exists(absolutePath).Should().BeTrue("the file should be physically saved to disk");
    }

    /// <summary>
    /// Test: Verify that empty folder path is handled correctly.
    /// Files should be saved to the root uploads folder.
    /// </summary>
    [Fact]
    public async Task SaveFileAsync_WithEmptyFolderPath_ShouldSaveToRootUploadsFolder()
    {
        // Arrange
        var mockFile = CreateMockFormFile();
        var folderPath = "";

        // Act
        var result = await _fileService.SaveFileAsync(mockFile.Object, folderPath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().StartWith("uploads/", "file should be in uploads folder");
    }

    #endregion

    #region SaveFileAsync Tests - File Size Validation

    /// <summary>
    /// Test: Verify that files exceeding the maximum size are rejected.
    /// Security: Prevents denial-of-service attacks through large file uploads.
    /// </summary>
    [Fact]
    public async Task SaveFileAsync_WithFileTooLarge_ShouldReturnFailure()
    {
        // Arrange: Create a file larger than the 5MB limit
        var fileSizeInBytes = 6L * 1024 * 1024;  // 6MB (exceeds 5MB limit)
        var mockFile = CreateMockFormFileWithSize(fileSizeInBytes);

        // Act
        var result = await _fileService.SaveFileAsync(mockFile.Object, "test");

        // Assert
        result.IsSuccess.Should().BeFalse("file exceeds maximum allowed size");
        result.Code.Should().Be("FILE_TOO_LARGE");
        result.Message.Should().Contain("5MB", "error message should indicate the limit");
    }

    /// <summary>
    /// Test: Verify that files at exactly the maximum size are accepted.
    /// Boundary condition test.
    /// </summary>
    [Fact]
    public async Task SaveFileAsync_WithFileAtMaxSize_ShouldReturnSuccess()
    {
        // Arrange: Create a file exactly at the 5MB limit
        var fileSizeInBytes = 5L * 1024 * 1024;  // Exactly 5MB
        var mockFile = CreateMockFormFileWithSize(fileSizeInBytes);

        // Act
        var result = await _fileService.SaveFileAsync(mockFile.Object, "test");

        // Assert
        result.IsSuccess.Should().BeTrue("file is exactly at the maximum allowed size");
    }

    #endregion

    #region SaveFileAsync Tests - File Extension Validation

    /// <summary>
    /// Test: Verify that files with disallowed extensions are rejected.
    /// Security: Prevents upload of potentially dangerous file types (e.g., .exe, .bat).
    /// </summary>
    [Theory]
    [InlineData("malware.exe")]        // Executable files
    [InlineData("script.bat")]         // Batch scripts
    [InlineData("dangerous.dll")]      // Dynamic link libraries
    [InlineData("config.config")]      // Configuration files
    [InlineData("shell.sh")]           // Shell scripts
    public async Task SaveFileAsync_WithDisallowedExtension_ShouldReturnFailure(string fileName)
    {
        // Arrange
        var mockFile = CreateMockFormFile(fileName, "content", "application/octet-stream");

        // Act
        var result = await _fileService.SaveFileAsync(mockFile.Object, "test");

        // Assert
        result.IsSuccess.Should().BeFalse($"'{Path.GetExtension(fileName)}' extension should be blocked");
        result.Code.Should().Be("INVALID_FILE_TYPE");
    }

    /// <summary>
    /// Test: Verify that all allowed extensions are accepted.
    /// </summary>
    [Theory]
    [InlineData("image.jpg", "image/jpeg")]
    [InlineData("image.jpeg", "image/jpeg")]
    [InlineData("image.png", "image/png")]
    [InlineData("document.pdf", "application/pdf")]
    public async Task SaveFileAsync_WithAllowedExtension_ShouldReturnSuccess(
        string fileName,
        string contentType)
    {
        // Arrange
        var mockFile = CreateMockFormFile(fileName, "content", contentType);

        // Act
        var result = await _fileService.SaveFileAsync(mockFile.Object, "test");

        // Assert
        result.IsSuccess.Should().BeTrue($"'{Path.GetExtension(fileName)}' extension should be allowed");
    }

    /// <summary>
    /// Test: Verify that extension validation is case-insensitive.
    /// Users may upload files with uppercase extensions (e.g., .JPG, .PNG).
    /// </summary>
    [Theory]
    [InlineData("IMAGE.JPG")]
    [InlineData("Image.Jpeg")]
    [InlineData("DOCUMENT.PDF")]
    public async Task SaveFileAsync_WithUppercaseExtension_ShouldReturnSuccess(string fileName)
    {
        // Arrange
        var mockFile = CreateMockFormFile(fileName, "content", "image/jpeg");

        // Act
        var result = await _fileService.SaveFileAsync(mockFile.Object, "test");

        // Assert
        result.IsSuccess.Should().BeTrue("extension validation should be case-insensitive");
    }

    #endregion

    #region SaveFileAsync Tests - MIME Type Validation

    /// <summary>
    /// Test: Verify that files with disallowed MIME types are rejected.
    /// Security: Double-checks file type to prevent extension spoofing attacks.
    /// </summary>
    [Theory]
    [InlineData("file.jpg", "text/html")]                    // HTML disguised as image
    [InlineData("file.pdf", "application/javascript")]       // JavaScript disguised as PDF
    [InlineData("file.png", "application/x-msdownload")]     // Executable disguised as image
    public async Task SaveFileAsync_WithDisallowedMimeType_ShouldReturnFailure(
        string fileName,
        string contentType)
    {
        // Arrange
        var mockFile = CreateMockFormFile(fileName, "content", contentType);

        // Act
        var result = await _fileService.SaveFileAsync(mockFile.Object, "test");

        // Assert
        result.IsSuccess.Should().BeFalse("MIME type should be validated");
        result.Code.Should().Be("INVALID_MIME_TYPE");
    }

    #endregion

    #region SaveFileAsync Tests - Path Traversal Prevention

    /// <summary>
    /// Test: Verify that path traversal attempts are blocked.
    /// Security: Prevents attackers from uploading files outside the uploads directory.
    /// </summary>
    [Theory]
    [InlineData("../../../etc")]           // Unix-style path traversal
    [InlineData("..\\..\\windows")]        // Windows-style path traversal
    [InlineData("....//....//")]           // Double-dot variations
    [InlineData("folder/../../../etc")]    // Mixed path traversal
    public async Task SaveFileAsync_WithPathTraversalAttempt_ShouldSanitizePath(string maliciousFolderPath)
    {
        // Arrange
        var mockFile = CreateMockFormFile();

        // Act
        var result = await _fileService.SaveFileAsync(mockFile.Object, maliciousFolderPath);

        // Assert: The file should either be saved safely or rejected
        // The sanitization should remove all ".." sequences
        if (result.IsSuccess)
        {
            result.Value.Should().NotContain("..", "path traversal sequences should be removed");
            result.Value.Should().StartWith("uploads/", "file should be within uploads folder");
        }
    }

    /// <summary>
    /// Test: Verify that absolute paths are rejected.
    /// Security: Prevents attackers from specifying arbitrary file system locations.
    /// </summary>
    [Theory]
    [InlineData("C:\\Windows\\System32")]
    [InlineData("E:\\ProtectedData")]
    [InlineData("D:\\Users")]
    public async Task SaveFileAsync_WithAbsolutePath_ShouldReturnFailure(string absolutePath)
    {
        // Arrange
        var mockFile = CreateMockFormFile();

        // Act
        var result = await _fileService.SaveFileAsync(mockFile.Object, absolutePath);

        // Assert
        result.IsSuccess.Should().BeFalse("absolute paths should be rejected");
        result.Code.Should().Be("INVALID_PATH");
    }

    /// <summary>
    /// Test: Verify that tilde (~) in paths is sanitized.
    /// Security: Home directory references could be exploited.
    /// </summary>
    [Fact]
    public async Task SaveFileAsync_WithTildeInPath_ShouldSanitize()
    {
        // Arrange
        var mockFile = CreateMockFormFile();

        // Act
        var result = await _fileService.SaveFileAsync(mockFile.Object, "~user/files");

        // Assert
        if (result.IsSuccess)
        {
            result.Value.Should().NotContain("~", "tilde should be removed");
        }
    }

    #endregion

    #region DeleteFileAsync Tests

    /// <summary>
    /// Test: Verify that an existing file can be deleted successfully.
    /// </summary>
    [Fact]
    public async Task DeleteFileAsync_WithExistingFile_ShouldReturnSuccess()
    {
        // Arrange: First, create a file to delete
        var mockFile = CreateMockFormFile();
        var saveResult = await _fileService.SaveFileAsync(mockFile.Object, "test-delete");
        saveResult.IsSuccess.Should().BeTrue("setup should succeed");

        // Act: Delete the file
        var deleteResult = await _fileService.DeleteFileAsync(saveResult.Value!);

        // Assert
        deleteResult.IsSuccess.Should().BeTrue("existing file should be deleted successfully");

        var absolutePath = _fileService.GetAbsolutePath(saveResult.Value!);
        File.Exists(absolutePath).Should().BeFalse("file should no longer exist on disk");
    }

    /// <summary>
    /// Test: Verify that deleting a non-existent file returns failure.
    /// </summary>
    [Fact]
    public async Task DeleteFileAsync_WithNonExistentFile_ShouldReturnNotFound()
    {
        // Arrange
        var relativePath = "uploads/non-existent-file.jpg";

        // Act
        var result = await _fileService.DeleteFileAsync(relativePath);

        // Assert
        result.IsSuccess.Should().BeFalse("non-existent file cannot be deleted");
        result.Code.Should().Be("NOT_FOUND");
    }

    /// <summary>
    /// Test: Verify that null or empty path returns failure.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeleteFileAsync_WithNullOrEmptyPath_ShouldReturnFailure(string? path)
    {
        // Act
        var result = await _fileService.DeleteFileAsync(path!);

        // Assert
        result.IsSuccess.Should().BeFalse("null or empty path should be rejected");
        result.Code.Should().Be("INVALID_PATH");
    }

    /// <summary>
    /// Test: Verify that deletion attempts outside uploads directory are blocked.
    /// Security: Prevents attackers from deleting arbitrary system files.
    /// </summary>
    [Theory]
    [InlineData("../../../important-system-file.txt")]
    [InlineData("..\\..\\windows\\system32\\config")]
    public async Task DeleteFileAsync_WithPathTraversalAttempt_ShouldReturnFailure(string maliciousPath)
    {
        // Act
        var result = await _fileService.DeleteFileAsync(maliciousPath);

        // Assert
        result.IsSuccess.Should().BeFalse("path traversal attempts should be blocked");
        result.Code.Should().Be("INVALID_PATH");
    }

    #endregion

    #region GetAbsolutePath Tests

    /// <summary>
    /// Test: Verify that relative paths with forward slashes are converted correctly.
    /// Web URLs use forward slashes, but Windows uses backslashes.
    /// </summary>
    [Fact]
    public void GetAbsolutePath_WithForwardSlashes_ShouldNormalizePath()
    {
        // Arrange
        var relativePath = "uploads/images/test.jpg";

        // Act
        var absolutePath = _fileService.GetAbsolutePath(relativePath);

        // Assert
        absolutePath.Should().StartWith(_testWebRootPath);
        absolutePath.Should().EndWith("test.jpg");
    }

    /// <summary>
    /// Test: Verify that relative paths with backslashes are converted correctly.
    /// </summary>
    [Fact]
    public void GetAbsolutePath_WithBackslashes_ShouldNormalizePath()
    {
        // Arrange
        var relativePath = "uploads\\images\\test.jpg";

        // Act
        var absolutePath = _fileService.GetAbsolutePath(relativePath);

        // Assert
        absolutePath.Should().StartWith(_testWebRootPath);
        absolutePath.Should().EndWith("test.jpg");
    }

    /// <summary>
    /// Test: Verify combining WebRootPath with relative path.
    /// </summary>
    [Fact]
    public void GetAbsolutePath_ShouldCombineWithWebRootPath()
    {
        // Arrange
        var relativePath = "uploads/test.jpg";
        var expectedPath = Path.Combine(_testWebRootPath, "uploads", "test.jpg");

        // Act
        var absolutePath = _fileService.GetAbsolutePath(relativePath);

        // Assert
        absolutePath.Should().Be(expectedPath);
    }

    #endregion

    #region Integration-style Tests

    /// <summary>
    /// Test: Verify the complete upload-then-delete workflow.
    /// This ensures the service works end-to-end.
    /// </summary>
    [Fact]
    public async Task CompleteWorkflow_UploadAndDelete_ShouldSucceed()
    {
        // Arrange
        var mockFile = CreateMockFormFile("workflow-test.pdf", "PDF content", "application/pdf");

        // Act - Upload
        var uploadResult = await _fileService.SaveFileAsync(mockFile.Object, "workflow-tests");

        // Assert - Upload succeeded
        uploadResult.IsSuccess.Should().BeTrue();
        var absolutePath = _fileService.GetAbsolutePath(uploadResult.Value!);
        File.Exists(absolutePath).Should().BeTrue();

        // Act - Delete
        var deleteResult = await _fileService.DeleteFileAsync(uploadResult.Value!);

        // Assert - Delete succeeded
        deleteResult.IsSuccess.Should().BeTrue();
        File.Exists(absolutePath).Should().BeFalse();
    }

    /// <summary>
    /// Test: Verify that generated filenames are unique.
    /// Multiple uploads of the same file should create unique filenames.
    /// </summary>
    [Fact]
    public async Task SaveFileAsync_MultipleUploads_ShouldGenerateUniqueFilenames()
    {
        // Arrange
        var mockFile1 = CreateMockFormFile("same-name.jpg");
        var mockFile2 = CreateMockFormFile("same-name.jpg");

        // Act
        var result1 = await _fileService.SaveFileAsync(mockFile1.Object, "test");
        var result2 = await _fileService.SaveFileAsync(mockFile2.Object, "test");

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Value.Should().NotBe(result2.Value, "each upload should have a unique filename");
    }

    #endregion
}
