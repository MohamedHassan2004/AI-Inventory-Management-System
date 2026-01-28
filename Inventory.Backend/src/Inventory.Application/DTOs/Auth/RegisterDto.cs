using Inventory.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace Inventory.Application.DTOs.Auth
{
    public class RegisterDto
    {
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public UserRole Role { get; set; }
    }
}