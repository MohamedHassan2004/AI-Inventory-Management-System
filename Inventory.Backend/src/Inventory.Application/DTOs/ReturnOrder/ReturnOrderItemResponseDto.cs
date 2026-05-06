using System;

namespace Inventory.Application.DTOs.ReturnOrder
{
    public class ReturnOrderItemResponseDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal RefundAmount { get; set; }
        public DateTime NewExpiryDate { get; set; }
    }
}
