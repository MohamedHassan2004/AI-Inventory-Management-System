using Inventory.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Inventory.API.Filter.Requirements
{
    public class StatusRequirement : IAuthorizationRequirement
    {
        public AccountStatus RequiredStatus { get; }

        public StatusRequirement(AccountStatus status)
        {
            RequiredStatus = status;
        }
    }
}
