using System;
using System.Collections.Generic;
using System.Text;

using Inventory.Application.DTOs.Reports.Users;

namespace Inventory.Application.Interfaces.Services;

public interface IUsersReportService
{
    Task<IEnumerable<CashierSalesDto>> GetCashierSalesAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken);
    Task<IEnumerable<UserStatusBreakdownDto>> GetUserStatusBreakdownAsync(
    CancellationToken cancellationToken);
}