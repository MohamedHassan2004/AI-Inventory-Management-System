using Inventory.Application.DTOs.Auth;
using Inventory.Application.Interfaces;
using Inventory.Application.Services;
using Inventory.Domain.Entities.Users;
using Inventory.Domain.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Inventory.Application.Test.Service;

public class UserServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IFileService> _fileServiceMock;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        // 1. Setup UserManager Mock with its complex dependencies
        var store = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _fileServiceMock = new Mock<IFileService>();
        _loggerMock = new Mock<ILogger<UserService>>();
        _localizationServiceMock = new Mock<ILocalizationService>();

        // Setup localization mock to return the key as the message
        _localizationServiceMock
            .Setup(l => l.GetMessage(It.IsAny<string>()))
            .Returns<string>(key => key);
        _localizationServiceMock
            .Setup(l => l.GetMessage(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns<string, object[]>((key, args) => string.Format(key, args));

        // 2. Initialize the System Under Test (SUT)
        _userService = new UserService(
            _userManagerMock.Object,
            _fileServiceMock.Object,
            _loggerMock.Object,
            _localizationServiceMock.Object);
    }

    #region UploadUserIdentityAsync Tests

    [Fact]
    public async Task UploadUserIdentityAsync_Success_ReturnsSuccessResult()
    {
        // Arrange
        var userId = "user-123";
        var user = new ApplicationUser { Id = userId, UserName = "Ahmed" };
        var fileMock = new Mock<IFormFile>();
        var dto = new UploadIdentityImgDto { IdentityImageFile = fileMock.Object };
        var savedUrl = "http://storage.com/identity.jpg";

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);

        _fileServiceMock.Setup(x => x.SaveFileAsync(fileMock.Object, "user-identities"))
            .ReturnsAsync(Result.Success<string>(savedUrl));

        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _userService.UploadUserIdentityAsync(userId, dto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(savedUrl, user.IdentityImgUrl);
        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task UploadUserIdentityAsync_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = "unknown";
        var dto = new UploadIdentityImgDto { IdentityImageFile = new Mock<IFormFile>().Object };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync((ApplicationUser)null!);

        // Act
        var result = await _userService.UploadUserIdentityAsync(userId, dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.ErrorCode);
        _fileServiceMock.Verify(x => x.SaveFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UploadUserIdentityAsync_FileSaveFails_ReturnsFileUploadError()
    {
        // Arrange
        var userId = "user-123";
        var user = new ApplicationUser { Id = userId };
        var fileMock = new Mock<IFormFile>();
        var dto = new UploadIdentityImgDto { IdentityImageFile = fileMock.Object };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);

        _fileServiceMock.Setup(x => x.SaveFileAsync(fileMock.Object, "user-identities"))
            .ReturnsAsync(Result.Failure<string>("ERR", "Disk Full"));

        // Act
        var result = await _userService.UploadUserIdentityAsync(userId, dto);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("FILE_UPLOAD_ERROR", result.ErrorCode);
        _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    #endregion

}