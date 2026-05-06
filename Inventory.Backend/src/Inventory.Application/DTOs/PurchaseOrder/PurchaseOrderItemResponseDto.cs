using System;

namespace Inventory.Application.DTOs.PurchaseOrder
{
    public class PurchaseOrderItemResponseDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public DateTime ExpiryDate { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
