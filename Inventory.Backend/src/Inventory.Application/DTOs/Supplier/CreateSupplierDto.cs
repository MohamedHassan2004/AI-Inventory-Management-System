namespace Inventory.Application.DTOs.Supplier
{
    public record CreateSupplierDto(string Name, string PhoneNumber, string? ContactInfo, string? Address);
}