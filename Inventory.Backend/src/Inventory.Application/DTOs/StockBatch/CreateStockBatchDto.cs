using System;

namespace Inventory.Application.DTOs.StockBatch
{
    public record CreateStockBatchDto(
        int ProductId,
        DateTime ExpireDate,
        decimal UnitCost,
        decimal OriginalQuantity,
        int SupplierId,
        decimal DiscountPercentage = 0
    );
}
