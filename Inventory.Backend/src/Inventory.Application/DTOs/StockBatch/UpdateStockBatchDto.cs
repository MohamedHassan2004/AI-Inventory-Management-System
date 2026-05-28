using System;

namespace Inventory.Application.DTOs.StockBatch
{
    public record UpdateStockBatchDto(
        DateTime ExpireDate,
        decimal UnitCost,
        decimal RemainingQuantity,
        decimal DiscountPercentage = 0
    );
}
