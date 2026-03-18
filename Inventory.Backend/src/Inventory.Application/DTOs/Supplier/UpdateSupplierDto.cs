namespace Inventory.Application.DTOs.Supplier
{
    public record UpdateSupplierDto(string Name, string PhoneNumber, string? ContactInfo, string? Address);
}