using Inventory.Domain.Enums;
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
        public string IdentityImgUrl { get; private set; } = string.Empty;
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; private set; } = null;
        public bool IsDeleted { get; private set; } = false;
        public DateTime? DeletedAt { get; private set; } = null;
        public AccountStatus AccountStatus { get; private set; } = AccountStatus.PendingChangePassword;
        public string? RejectionReason { get; private set; } = null;

        public ApplicationUser() { }

        public ApplicationUser(
            string userName,
            string fullName,
            string email,
            string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(userName)) throw new ArgumentException("Username is required");
            if (string.IsNullOrWhiteSpace(fullName)) throw new ArgumentException("Full name is required");
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required");
            if (!email.Contains('@')) throw new ArgumentException("Email is not correct.");
            if (string.IsNullOrWhiteSpace(phoneNumber)) throw new ArgumentException("Phone number is required");

            UserName = userName;
            Email = email;
            FullName = fullName;
            PhoneNumber = phoneNumber;
        }

        public void MarkAsDeleted(IDateTimeProvider timeProvider)
        {
            IsDeleted = true;
            DeletedAt = timeProvider.UtcNow;
            AccountStatus = AccountStatus.Deleted;
        }
        public void Restore()
        {
            IsDeleted = false;
            DeletedAt = null;
            AccountStatus = AccountStatus.Active;
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
            AccountStatus = AccountStatus.PendingIdentityUpload;
        }
        public void ApproveAccount()
        {
            AccountStatus = AccountStatus.Active;
        }
        public void RejectAccount(string reason)
        {
            AccountStatus = AccountStatus.Rejected;
            RejectionReason = reason;
        }

        public void SetIdentityImgUrl(string url)
        {
            IdentityImgUrl = url;
            AccountStatus = AccountStatus.PendingAdminReview;
        }
    }
}
