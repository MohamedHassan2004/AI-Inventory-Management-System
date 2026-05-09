using System;
using System.Collections.Generic;
using System.Text;

using Inventory.Application.DTOs.Reports.Returns;

namespace Inventory.Application.Interfaces.Services;

public interface IReturnsReportService
{
    Task<ReturnsSummaryDto> GetReturnsSummaryAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken);
    Task<IEnumerable<TopReturnedProductDto>> GetTopReturnedProductsAsync(
    DateTime startDate,
    DateTime endDate,
    int top,
    CancellationToken cancellationToken);
}