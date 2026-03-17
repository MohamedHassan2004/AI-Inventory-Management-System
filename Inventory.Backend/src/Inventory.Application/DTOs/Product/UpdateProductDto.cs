using System;

namespace Inventory.Application.DTOs.Product
{
    public record UpdateProductDto(
        string SKU,
        string Name,
        int? CategoryId
    );
}
