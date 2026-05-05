using Inventory.Domain.Entities.Users;
using Xunit;

namespace Inventory.Domain.Test.Entities.Users
{
    public class RefreshTokenTests
    {
        [Fact]
        public void CanCreateRefreshToken()
        {
            var token = new RefreshToken();
            Assert.NotNull(token);
        }

        [Fact]
        public void RevokeRefreshToken_WhenCalled_ShouldSetIsRevokedAndRevokedOn()
        {
            var token = new RefreshToken();
            Assert.False(token.IsRevoked);
            Assert.Null(token.RevokedOn);
            token.RevokeRefreshToken();
            Assert.True(token.IsRevoked);
            Assert.NotNull(token.RevokedOn);
            Assert.True((System.DateTime.UtcNow - token.RevokedOn.Value).TotalSeconds < 2);
        }

        [Fact]
        public void RefreshToken_DefaultValues_ShouldBeCorrect()
        {
            var token = new RefreshToken();
            Assert.Equal(0, token.Id);
            Assert.Null(token.Token);
            Assert.Equal(default, token.ExpiryDate);
            Assert.False(token.IsRevoked);
            Assert.Null(token.RevokedOn);
            Assert.True((System.DateTime.UtcNow - token.CreatedAt).TotalSeconds < 2);
            Assert.Null(token.UserId);
            Assert.Null(token.User);
        }
    }
}
