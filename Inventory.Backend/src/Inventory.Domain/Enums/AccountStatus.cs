using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Domain.Enums
{
    public enum AccountStatus
    {
        PendingChangePassword = 0,
        PendingIdentityUpload,
        PendingAdminReview,
        Rejected,
        Active,
        Deleted
    }
}
