using Inventory.Domain.Shared;

namespace Inventory.Application.DTOs.Reports.Purchases;

public class SupplierPurchasesReportDto
{
    public PagedResult<SupplierPurchasesReportItemDto> Suppliers { get; set; } = default!;
}
