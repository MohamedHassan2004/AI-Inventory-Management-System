using System;

namespace Inventory.Application.DTOs.Product
{
    public record UpdateProductReorderPointDto(
        int ReorderPoint
    );
}
