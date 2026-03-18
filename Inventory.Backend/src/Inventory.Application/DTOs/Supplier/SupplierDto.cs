namespace Inventory.Application.DTOs.Supplier
{
    public record SupplierDto(int Id, string Name, string PhoneNumber, string? ContactInfo, string? Address, int TotalRating, int RatingCount, double AvgRating, int DeliveryCount, double AvgDeliveryTime);
}