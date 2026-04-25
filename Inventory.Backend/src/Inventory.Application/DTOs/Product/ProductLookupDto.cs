namespace Inventory.Application.DTOs.Product
{
    /// <summary>
    /// Lightweight DTO returned by the product search endpoint.
    /// Designed for the cashier product-picker screen.
    /// </summary>
    public record ProductLookupDto(
        int Id,
        string SKU,
        string Name,
        decimal SellingPrice,
        decimal AvailableQuantity
    );
}
