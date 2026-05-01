using Inventory.Domain.Enums;

namespace Inventory.Application.DTOs.Order
{
    
    public class SubmitOrderDto
    {
        public List<OrderItemDto> Items { get; set; } = new();
        public PaymentMethod PaymentMethod { get; set; }
        public OrderType OrderType { get; set; }

        
        public decimal DiscountPercentage { get; set; }
    }
}
