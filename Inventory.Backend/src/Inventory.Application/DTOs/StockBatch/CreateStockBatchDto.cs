using System;

namespace Inventory.Application.DTOs.StockBatch
{
    public record CreateStockBatchDto(
        int ProductId,
        DateTime PurchaseDate,
        DateTime ExpireDate,
        decimal UnitCost,
        decimal OriginalQuantity,
        int SupplierId
    );
}
