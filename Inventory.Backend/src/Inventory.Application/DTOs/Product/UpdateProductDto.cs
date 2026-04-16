using System;

namespace Inventory.Application.DTOs.Product
{
    public record UpdateProductDto(
        string Name,
        int? CategoryId
    );
}
