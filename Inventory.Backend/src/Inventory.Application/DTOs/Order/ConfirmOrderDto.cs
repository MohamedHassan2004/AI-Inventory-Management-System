using Inventory.Domain.Enums;

namespace Inventory.Application.DTOs.Order
{
    public class ConfirmOrderDto
    {
        public PaymentMethod PaymentMethod { get; set; }
        public OrderType OrderType { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public string RowVersion { get; set; } = string.Empty; // Base64 encoded byte array
    }
}
