using Inventory.Application.DTOs.Supplier;
using Inventory.Domain.Shared;

namespace Inventory.Application.Interfaces.Queries.Reports;

public interface ISupplierReportQuery
{
    Task<PagedResult<SupplierReportItemDto>> GetSuppliersReportAsync(
        DateTime? startDate,
        DateTime? endDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}