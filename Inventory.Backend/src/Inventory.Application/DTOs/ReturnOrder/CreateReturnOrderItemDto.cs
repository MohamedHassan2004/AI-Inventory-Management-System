using System;

namespace Inventory.Application.DTOs.ReturnOrder
{
    public class CreateReturnOrderItemDto
    {
        public int OriginalOrderItemId { get; set; }
        public decimal Quantity { get; set; }
        public DateTime? NewExpiryDate { get; set; }
    }
}
