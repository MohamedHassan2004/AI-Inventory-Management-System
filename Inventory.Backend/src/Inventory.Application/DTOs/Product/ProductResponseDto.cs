using System;
using Inventory.Application.DTOs.Category;

namespace Inventory.Application.DTOs.Product
{
    public record ProductResponseDto(
        int Id,
        string SKU,
        string Name,
        decimal SellingPrice,
        decimal StockQuantity,
        int ReorderPoint,
        CategoryResponseDto? Category
    );
}
