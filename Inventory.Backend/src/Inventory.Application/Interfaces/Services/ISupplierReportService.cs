using Inventory.Application.DTOs.Supplier;
using Inventory.Domain.Shared;

namespace Inventory.Application.Interfaces.Services;

public interface ISupplierReportService
{
    Task<PagedResult<SupplierReportItemDto>> GetSuppliersReportAsync(
        DateTime? startDate,
        DateTime? endDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}