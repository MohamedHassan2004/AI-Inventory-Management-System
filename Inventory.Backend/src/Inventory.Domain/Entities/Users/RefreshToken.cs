
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Domain.Entities.Users
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string Token { get; set; } = null!;
        public DateTime ExpiryDate { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime? RevokedOn { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;

        public void RevokeRefreshToken()
        {
            IsRevoked = true;
            RevokedOn = DateTime.UtcNow;
        }
    }
}
