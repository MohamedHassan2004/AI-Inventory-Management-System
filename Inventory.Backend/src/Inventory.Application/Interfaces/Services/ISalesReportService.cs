using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.DTOs.Reports.Sales;

namespace Inventory.Application.Interfaces.Services;

public interface ISalesReportService
{
    Task<SalesSummaryDto> GetSalesSummaryAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken);
    Task<IEnumerable<SalesTopProductDto>> GetTopSellingProductsAsync(
    DateTime startDate,
    DateTime endDate,
    int top,
    CancellationToken cancellationToken);
    Task<SalesAnalyticsDto> GetSalesAnalyticsAsync(
    DateTime startDate,
    DateTime endDate,
    CancellationToken cancellationToken);
}