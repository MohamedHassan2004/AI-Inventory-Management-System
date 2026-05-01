using System.Collections.Generic;

namespace Inventory.Application.DTOs.PurchaseOrder
{
    public class SubmitPurchaseOrderDto
    {
        public int SupplierId { get; set; }
        public List<PurchaseOrderItemDto> Items { get; set; } = new();
    }
}
