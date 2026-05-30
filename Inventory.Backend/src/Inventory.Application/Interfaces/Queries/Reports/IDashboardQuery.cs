using System;
using System.Collections.Generic;
using System.Text;
using Inventory.Application.DTOs.Reports.Dashboard;

namespace Inventory.Application.Interfaces.Queries.Reports;

public interface IDashboardQuery
{
    Task<DashboardSummaryDto> GetSummaryAsync(
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken);
}