using Inventory.Domain.Entities.Users;
using Inventory.Domain.Interfaces;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Inventory.Domain.Test.Entities.Users
{
    public class ApplicationUserTests
    {
        private readonly ApplicationUser _user;
        private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;

        public ApplicationUserTests()
        {
            _user = new ApplicationUser();
            _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        }

        [Fact]
        public void NewUser_ShouldBeInitializedWithCorrectDefaultValues()
        {
            // Assert
            Assert.False(_user.IsDeleted);
            Assert.Null(_user.DeletedAt);
            Assert.Null(_user.LastLoginAt);
            Assert.True(_user.MustChangePassword);
            Assert.Null(_user.RefreshToken);
        }

        [Theory]
        [InlineData(null, "Full Name", "email@test.com", "0123", "url")]
        [InlineData("username", "", "email@test.com", "0123", "url")]
        [InlineData("username", "Full Name", " ", "0123", "url")]
        public void Constructor_WhenInvalidDataProvided_ShouldThrowArgumentException(
            string user, string name, string mail, string phone, string img)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                new ApplicationUser(user, name, mail, phone, img, UserRole.Cashier));
        }

        [Fact]
        public void Constructor_WhenValidDataProvided_ShouldInitializePropertiesCorrect()
        {
            // Arrange
            var userName = "johndoe";
            var fullName = "John Doe";
            var email = "john@example.com";
            var phone = "01000000000";
            var img = "path/to/img.png";
            var role = UserRole.Manager;

            // Act
            var newUser = new ApplicationUser(userName, fullName, email, phone, img, role);

            // Assert
            Assert.Equal(userName, newUser.UserName);
            Assert.Equal(fullName, newUser.FullName);
            Assert.Equal(email, newUser.Email);
            Assert.Equal(phone, newUser.PhoneNumber);
            Assert.Equal(img, newUser.IdentityImgUrl);
            Assert.Equal(role, newUser.Role);
        }

        [Fact]
        public void MarkAsDeleted_WhenCalled_ShouldMakeIsDeletedTrueAndUpdateDeletedAt()
        {
            // Arrange
            var expectedDateTime = new DateTime(2026, 1, 1, 10, 0, 0);
            _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(expectedDateTime);
            // Act
            _user.MarkAsDeleted(_dateTimeProviderMock.Object);
            // Assert
            Assert.True(_user.IsDeleted);
            Assert.Equal(expectedDateTime, _user.DeletedAt);
        }


        [Fact]
        public void UpdateLastLogin_WhenCalled_ShouldSetLastLoginAtToCurrentTime()
        {
            // Arrange
            var expectedTime = new DateTime(2026, 1, 1, 10, 0, 0);
            _dateTimeProviderMock.Setup(tp => tp.UtcNow).Returns(expectedTime);
            // Act
            _user.UpdateLastLogin(_dateTimeProviderMock.Object);
            // Assert
            Assert.NotNull(_user.LastLoginAt);
            Assert.Equal(expectedTime, _user.LastLoginAt);
        }

        [Fact]
        public void Login_WhenUserNotDeleted_ShouldUpdateLastLoginAt()
        {
            // Arrange
            var expectedTime = new DateTime(2026, 1, 1, 10, 0, 0);
            _dateTimeProviderMock.Setup(tp => tp.UtcNow).Returns(expectedTime);
            // Act 
            _user.Login(_dateTimeProviderMock.Object);
            // Assert
            Assert.NotNull(_user.LastLoginAt);
            Assert.Equal(expectedTime, _user.LastLoginAt);
        }

        [Fact]
        public void Login_WhenUserDeleted_ShouldThrowInvalidOperationException()
        {
            // Arrange
            _user.MarkAsDeleted(_dateTimeProviderMock.Object);
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _user.Login(_dateTimeProviderMock.Object));
            Assert.Equal("Cannot login a deleted user.", exception.Message);
        }

        [Fact]
        public void PasswordChanged_WhenCalled_ShouldSetMustChangePasswordToFalse()
        {
            // Act
            _user.PasswordChanged();
            // Assert
            Assert.False(_user.MustChangePassword);
        }

        [Fact]
        public void ChangeRole_WhenUserDeleted_ShouldThrowInvalidOperationException()
        {
            // Arrange
            _user.MarkAsDeleted(_dateTimeProviderMock.Object);
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _user.ChangeRole(UserRole.Cashier));
            Assert.Equal("Cannot change role of a deleted user.", exception.Message);
        }

        [Fact]
        public void ChangeRole_WhenNewRoleIsSuperAdmin_ShouldThrowInvalidOperationException()
        {
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _user.ChangeRole(UserRole.SuperAdmin));
            Assert.Equal("SuperAdmin role can only be assigned through system initialization.", exception.Message);
        }

        [Fact]
        public void ChangeRole_WhenCalledWithNotValidRole_ShouldThrowArgumentException()
        {
            // Arrange
            var invalidRole = (UserRole)999;
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _user.ChangeRole(invalidRole));
            Assert.Equal("Invalid user role", exception.Message);
        }

        [Fact]
        public void ChangeRole_WhenCalledWithValidRole_ShouldUpdateUserRole()
        {
            // Arrange
            var newRole = UserRole.Manager;
            // Act
            _user.ChangeRole(newRole);
            // Assert
            Assert.Equal(newRole, _user.Role);
        }

        [Fact]
        public void SetRefreshToken_WhenCalled_ShouldUpdateTokenAndExpiry()
        {
            // Arrange
            var token = "sample-refresh-token";
            var expiry = DateTime.UtcNow.AddDays(7);

            // Act
            _user.SetRefreshToken(token, expiry);

            // Assert
            Assert.Equal(token, _user.RefreshToken);
            Assert.Equal(expiry, _user.RefreshTokenExpiresAt);
        }

        [Fact]
        public void RevokeRefreshToken_WhenCalled_ShouldSetTokenAndExpiryToNull()
        {
            // Arrange
            _user.SetRefreshToken("token", DateTime.UtcNow);

            // Act
            _user.RevokeRefreshToken();

            // Assert
            Assert.Null(_user.RefreshToken);
            Assert.Null(_user.RefreshTokenExpiresAt);
        }

    }
}
