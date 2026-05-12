using Inventory.Domain.Shared;

namespace Inventory.Application.DTOs.Supplier;

public class SupplierReportDto
{
    public PagedResult<SupplierReportItemDto> Suppliers { get; set; } = default!;
}
