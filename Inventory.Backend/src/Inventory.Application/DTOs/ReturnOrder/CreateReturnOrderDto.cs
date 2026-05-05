using System.Collections.Generic;

namespace Inventory.Application.DTOs.ReturnOrder
{
    public class CreateReturnOrderDto
    {
        public int OriginalOrderId { get; set; }
        public string? Reason { get; set; }
        public List<CreateReturnOrderItemDto> Items { get; set; } = new();
    }
}
