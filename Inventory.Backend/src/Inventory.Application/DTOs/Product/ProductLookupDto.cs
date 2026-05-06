namespace Inventory.Application.DTOs.Product
{
    
    public record ProductLookupDto(
        int Id,
        string SKU,
        string Name,
        decimal SellingPrice,
        decimal AvailableQuantity
    );
}
