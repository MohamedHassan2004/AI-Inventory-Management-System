namespace Inventory.Application.DTOs.Order
{
    public class AddOrderItemDto
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal DiscountPercentage { get; set; }
    }
}
