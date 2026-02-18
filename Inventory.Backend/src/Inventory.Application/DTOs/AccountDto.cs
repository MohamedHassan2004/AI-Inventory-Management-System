using Inventory.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.DTOs
{
    public class AccountDto
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string IdentityImgUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public AccountStatus AccountStatus { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();
    }
}
