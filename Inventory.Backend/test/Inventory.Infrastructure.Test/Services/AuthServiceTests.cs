using Inventory.Application.DTOs.Auth;
using Inventory.Application.Interfaces;
using Inventory.Application.Services;
using Inventory.Domain.Entities.Users;
using Inventory.Domain.Interfaces;
using Inventory.Infrastructure.Services.Auth;
using Microsoft.AspNetCore.Identity;
using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Microsoft.Extensions.Options;
using Inventory.Domain.Settings;

namespace Inventory.Infrastructure.Test.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<AuthService>> _loggerMock;
        private readonly Mock<IDateTimeProvider> _dateTimeProvider;
        private readonly Mock<IOptions<JwtSettings>> _jwtSettingsMock;
        private readonly Mock<ILocalizationService> _localizationServiceMock;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            var contextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            var userPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
            _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
                _userManagerMock.Object,
                contextAccessor.Object,
                userPrincipalFactory.Object,
                null!, null!, null!, null!);

            var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(dbOptions);

            _loggerMock = new Mock<ILogger<AuthService>>();
            _dateTimeProvider = new Mock<IDateTimeProvider>();
            _jwtSettingsMock = new Mock<IOptions<JwtSettings>>();
            _localizationServiceMock = new Mock<ILocalizationService>();

            // Setup localization mock to return the key as the message
            _localizationServiceMock
                .Setup(l => l.GetMessage(It.IsAny<string>()))
                .Returns<string>(key => key);
            _localizationServiceMock
                .Setup(l => l.GetMessage(It.IsAny<string>(), It.IsAny<object[]>()))
                .Returns<string, object[]>((key, args) => string.Format(key, args));

            _authService = new AuthService(
                _userManagerMock.Object,
                _signInManagerMock.Object,
                _context,
                _loggerMock.Object,
                _dateTimeProvider.Object,
                _jwtSettingsMock.Object,
                _localizationServiceMock.Object);

        }

        #region ChangePasswordAsync Tests

        [Fact]
        public async Task ChangePasswordAsync_WhenUserExistsAndPasswordIsCorrect_ShouldReturnSuccess()
        {
            // Arrange
            var userId = "user-123";
            var user = new ApplicationUser { Id = userId, UserName = "testuser" };
            var dto = new ChangePasswordDto
            {
                CurrentPassword = "Old123!",
                NewPassword = "New123!",
            };

            _userManagerMock.Setup(x => x.FindByIdAsync(userId))
                .ReturnsAsync(user);

            _userManagerMock.Setup(x => x.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _authService.ChangePasswordAsync(userId, dto);

            // Assert
            Assert.True(result.IsSuccess);


            _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
        }


        [Fact]
        public async Task ChangePasswordAsync_WhenUserDoesNotExist_ShouldReturnNotFoundFailure()
        {
            // arrange
            var userId = "nonexistent-user";
            var dto = It.IsAny<ChangePasswordDto>();
            _userManagerMock.Setup(x => x.FindByIdAsync(userId))
                .ReturnsAsync((ApplicationUser)null!);

            // act
            var result = await _authService.ChangePasswordAsync(userId, dto);
            // assert
            Assert.False(result.IsSuccess);
            Assert.Equal("NOT_FOUND", result.Error.Code);

            _userManagerMock.Verify(x => x.ChangePasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task ChangePasswordAsync_WhenPasswordIsIncorrect_ShouldReturnFailure()
        {
            // Arrange
            var userId = "user-123";
            var user = new ApplicationUser { Id = userId, UserName = "testuser" };
            var dto = new ChangePasswordDto { CurrentPassword = "WrongPassword", NewPassword = "New123!" };

            _userManagerMock.Setup(u => u.FindByIdAsync(userId))
                .ReturnsAsync(user);

            var identityError = new IdentityError { Description = "Invalid password." };
            _userManagerMock.Setup(x => x.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword))
                .ReturnsAsync(IdentityResult.Failed(identityError));

            // Act 
            var result = await _authService.ChangePasswordAsync(userId, dto);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("PASSWORD_CHANGE_FAILED", result.Error.Code);
            Assert.Contains("Invalid password.", result.Error.Description);

            _userManagerMock.Verify(x => x.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword), Times.Once);
            _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        #endregion

    }
}
