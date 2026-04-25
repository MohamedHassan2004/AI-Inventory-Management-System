using Inventory.Domain.Enums;

namespace Inventory.Application.DTOs.Order
{
    /// <summary>
    /// Carries the full order from the frontend to the backend in a single request.
    /// The cashier builds this locally (using product search results) and submits once.
    /// </summary>
    public class SubmitOrderDto
    {
        public List<OrderItemDto> Items { get; set; } = new();
        public PaymentMethod PaymentMethod { get; set; }
        public OrderType OrderType { get; set; }

        /// <summary>0 = no discount. Max 70%.</summary>
        public decimal DiscountPercentage { get; set; }
    }
}
