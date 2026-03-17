using System;

namespace Inventory.Application.DTOs.Product
{
    public record UpdateProductPriceDto(
        decimal SellingPrice
    );
}
