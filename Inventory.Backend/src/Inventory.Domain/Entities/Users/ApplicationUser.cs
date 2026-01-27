using Inventory.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using System;
using System.Data;
using System.Runtime.CompilerServices;
using System.Security.Principal;

namespace Inventory.Domain.Entities.Users
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string IdentityImgUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public UserRole Role { get; private set; }
        public DateTime? LastLoginAt { get; private set; } = null;
        public bool IsDeleted { get; private set; } = false;
        public DateTime? DeletedAt { get; private set; } = null;
        public bool MustChangePassword { get; private set; } = true;
        public string? RefreshToken { get; private set; } = null;
        public DateTime? RefreshTokenExpiresAt { get; private set; }


        public ApplicationUser() { }

        public ApplicationUser(
            string userName,
            string fullName,
            string email,
            string phoneNumber,
            string identityImgUrl,
            UserRole role)
        {
            if (string.IsNullOrEmpty(userName)) throw new ArgumentException("Username is required");
            if (string.IsNullOrWhiteSpace(fullName)) throw new ArgumentException("Full name is required");
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required");
            if (string.IsNullOrWhiteSpace(phoneNumber)) throw new ArgumentException("Phone number is required");
            if (string.IsNullOrWhiteSpace(identityImgUrl)) throw new ArgumentException("Identity image URL is required");
            if (!Enum.IsDefined(typeof(UserRole), role)) throw new ArgumentException("Invalid user role");

            UserName = userName;
            Email = email;
            FullName = fullName;
            Role = role;
            PhoneNumber = phoneNumber;
            IdentityImgUrl = identityImgUrl;
        }

        public void MarkAsDeleted(IDateTimeProvider timeProvider)
        {
            IsDeleted = true;
            DeletedAt = timeProvider.UtcNow;
        }
        public void UpdateLastLogin(IDateTimeProvider timeProvider)
        {
            LastLoginAt = timeProvider.UtcNow;
        }
        public void Login(IDateTimeProvider timeProvider)
        {
            if (IsDeleted)
            {
                throw new InvalidOperationException("Cannot login a deleted user.");
            }
            UpdateLastLogin(timeProvider);
        }
        public void PasswordChanged()
        {
            MustChangePassword = false;
        }
        public void ChangeRole(UserRole newRole)
        {
            if (IsDeleted)
                throw new InvalidOperationException("Cannot change role of a deleted user.");

            if (newRole == UserRole.SuperAdmin)
                throw new InvalidOperationException("SuperAdmin role can only be assigned through system initialization.");

            if (!Enum.IsDefined<UserRole>(newRole)) 
                throw new ArgumentException("Invalid user role");

            Role = newRole;
        }
        public void SetRefreshToken(string token, DateTime expiresAt)
        {
            RefreshToken = token;
            RefreshTokenExpiresAt = expiresAt;
        }
        public void RevokeRefreshToken()
        {
            RefreshToken = null;
            RefreshTokenExpiresAt = null;
        }
    }
}
