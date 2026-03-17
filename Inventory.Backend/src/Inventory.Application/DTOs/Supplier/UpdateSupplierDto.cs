namespace Inventory.Application.DTOs.Supplier
{
    public record UpdateSupplierDto(int Id, string Name, string PhoneNumber, string? ContactInfo, string? Address);
}