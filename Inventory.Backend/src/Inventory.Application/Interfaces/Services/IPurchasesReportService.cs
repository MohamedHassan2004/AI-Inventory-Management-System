using System;
using System.Collections.Generic;
using System.Text;
using Inventory.Application.DTOs.Reports.Purchases;

namespace Inventory.Application.Interfaces.Services;

public interface IPurchasesReportService
{
    Task<PurchasesSummaryDto> GetPurchasesSummaryAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken);
    Task<IEnumerable<TopSupplierDto>> GetTopSuppliersAsync(
    DateTime startDate,
    DateTime endDate,
    int top,
    CancellationToken cancellationToken);
}