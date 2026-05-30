using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Application.DTOs.Reports.Sales;
using Inventory.Domain.Shared;

namespace Inventory.Application.Interfaces.Queries.Reports;

public interface ISalesReportQuery
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
    Task<PagedResult<ProfitMarginDto>> GetProfitMarginsAsync(
    DateTime startDate,
    DateTime endDate,
    int page,
    int pageSize,
    CancellationToken cancellationToken);
}