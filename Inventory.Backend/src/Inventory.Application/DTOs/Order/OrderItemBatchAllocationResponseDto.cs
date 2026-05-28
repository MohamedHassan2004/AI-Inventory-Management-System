using System;

namespace Inventory.Application.DTOs.Order
{
    public class OrderItemBatchAllocationResponseDto
    {
        public int StockBatchId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal FinalPrice => UnitPrice * (1 - DiscountPercentage / 100m);
        public decimal Total => Quantity * FinalPrice;
    }
}
