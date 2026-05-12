using Inventory.Domain.Shared;
using Inventory.Application.DTOs.Reports.Purchases;

namespace Inventory.Application.Interfaces.Services;

public interface IPurchasesReportService
{
    Task<PurchasesSummaryDto> GetPurchasesSummaryAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken);

    Task<PagedResult<SupplierPurchasesReportItemDto>> GetSuppliersReportAsync(
        DateTime startDate,
        DateTime endDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}