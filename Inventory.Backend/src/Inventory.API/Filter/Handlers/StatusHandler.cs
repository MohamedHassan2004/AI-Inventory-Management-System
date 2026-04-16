using Inventory.API.Controllers;
using Inventory.API.Filter.Requirements;
using Inventory.Application.Interfaces;
using Inventory.Application.Services;
using Inventory.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Inventory.API.Filter.Handlers
{
    public class StatusHandler : AuthorizationHandler<StatusRequirement>
    {
        private readonly IUserService _userService;

        public StatusHandler(IUserService userService)
        {
            _userService = userService;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, StatusRequirement requirement)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return; 
            }

            var userStatusResult = await _userService.GetUserStatusAsync(userId);
            if (!userStatusResult.IsSuccess)
                return;

            if (Enum.TryParse<AccountStatus>(userStatusResult.Value, out var status))
            {
                if (requirement.AllowedStatuses.Contains(status))
                {
                    context.Succeed(requirement);
                }
            }
        }
    }
}
