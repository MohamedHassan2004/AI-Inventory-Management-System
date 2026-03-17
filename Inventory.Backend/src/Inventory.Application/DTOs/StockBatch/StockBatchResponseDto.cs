using System;

namespace Inventory.Application.DTOs.StockBatch
{
    public record StockBatchResponseDto(
        int Id,
        int ProductId,
        DateTime PurchaseDate,
        DateTime ExpireDate,
        decimal UnitCost,
        decimal OriginalQuantity,
        decimal RemainingQuantity,
        int SupplierId
    );
}
