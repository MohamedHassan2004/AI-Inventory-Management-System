using Inventory.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Inventory.API.Filter.Requirements
{
    public class StatusRequirement : IAuthorizationRequirement
    {
        public List<AccountStatus> AllowedStatuses { get; }

        public StatusRequirement(params AccountStatus[] statuses)
        {
            AllowedStatuses = statuses.ToList();
        }
    }
}
