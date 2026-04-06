using Inventory.Domain.Entities.Users;
using Inventory.Domain.Enums;
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

        #region NewUser

        [Fact]
        public void NewUser_ShouldBeInitializedWithCorrectDefaultValues()
        {
            // Assert
            Assert.False(_user.IsDeleted);
            Assert.Null(_user.DeletedAt);
            Assert.Null(_user.LastLoginAt);
            Assert.Null(_user.RejectionReason);
        }

        [Theory]
        [InlineData("", "Full Name", "email@test.com", "0123")]
        [InlineData("username", " ", "email@test.com", "0123")]
        [InlineData("username", " ", "email$test.com", "0123")]
        [InlineData("username", "Full Name", " ", "0123")]
        [InlineData("username", "Full Name", "email@test.com", " ")]
        public void Constructor_WhenInvalidDataProvided_ShouldThrowArgumentException(
            string user, string name, string mail, string phone)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                new ApplicationUser(user, name, mail, phone));
        }

        [Fact]
        public void Constructor_WhenValidDataProvided_ShouldInitializePropertiesCorrect()
        {
            // Arrange
            var userName = "johndoe";
            var fullName = "John Doe";
            var email = "john@example.com";
            var phone = "01000000000";

            // Act
            var newUser = new ApplicationUser(userName, fullName, email, phone);

            // Assert
            Assert.Equal(userName, newUser.UserName);
            Assert.Equal(fullName, newUser.FullName);
            Assert.Equal(email, newUser.Email);
            Assert.Equal(phone, newUser.PhoneNumber);
        }
        #endregion

        #region  MarkAsDeleted
        [Fact]
        public void MarkAsDeleted_WhenCalled_ShouldBehaveCorrectly()
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

        #endregion

        #region  Restore
        [Fact]
        public void Restore_WhenCalled_ShouldBehaveCorrectly()
        {
            // Arrange
            var deletionTime = new DateTime(2026, 1, 1, 10, 0, 0);
            _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(deletionTime);
            _user.MarkAsDeleted(_dateTimeProviderMock.Object);
            // Act
            _user.Restore();
            // Assert
            Assert.False(_user.IsDeleted);
            Assert.Null(_user.DeletedAt);
        }

        #endregion

        #region  UpdateLastLogin
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

        #endregion

        #region Login
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

        #endregion

        #region PasswordChanged
        [Fact]
        public void PasswordChanged_WhenCalled_ShouldSetMustChangePasswordToFalse()
        {
            // Act
            _user.PasswordChanged();
            // Assert
        }
        #endregion

        #region SetIdentityImg
        [Fact]
        public void SetIdentityImgUrl_WhenCalled_ShouldUpdateIdentityImgUrl()
        {
            // Arrange
            var newUrl = "http://example.com/identity.jpg";
            // Act
            _user.SetIdentityImgUrl(newUrl);
            // Assert
            Assert.Equal(newUrl, _user.IdentityImgUrl);
        }

        #endregion

        #region Account Status Management
        [Fact]
        public void ApproveAccount_WhenCalled_ShouldSetAccountStatusToApproved()
        {
            // Act
            _user.ApproveAccount();
            // Assert
        }

        [Fact]
        public void RejectAccount_WhenCalled_ShouldSetAccountStatusToRejectedAndSetRejectionReason()
        {
            // Arrange
            string rejectionReason = "Insufficient documentation";
            // Act
            _user.RejectAccount(rejectionReason);
            // Assert
            Assert.Equal(AccountStatus.Rejected, _user.AccountStatus);
            Assert.NotNull(_user.RejectionReason);
            Assert.Equal(rejectionReason, _user.RejectionReason);
        }

        #endregion

    }
}
