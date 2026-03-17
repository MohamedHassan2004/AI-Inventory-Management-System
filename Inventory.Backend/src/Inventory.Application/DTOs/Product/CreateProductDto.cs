using System;

namespace Inventory.Application.DTOs.Product
{
    public record CreateProductDto(
        string SKU,
        string Name,
        decimal SellingPrice,
        int ReorderPoint,
        int? CategoryId
    );
}
