using System;
using System.Collections.Generic;
using System.Text;
using Inventory.Application.DTOs.Dashboard;

namespace Inventory.Application.Interfaces.Services;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(
        CancellationToken cancellationToken);
}