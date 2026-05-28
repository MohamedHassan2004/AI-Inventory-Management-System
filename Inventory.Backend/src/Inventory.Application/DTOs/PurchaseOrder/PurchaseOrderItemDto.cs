using System;

namespace Inventory.Application.DTOs.PurchaseOrder
{
    public class PurchaseOrderItemDto
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public DateTime ExpiryDate { get; set; }
        public decimal DiscountPercentage { get; set; }
    }
}
