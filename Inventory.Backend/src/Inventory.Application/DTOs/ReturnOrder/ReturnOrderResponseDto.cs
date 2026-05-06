using System;
using System.Collections.Generic;

namespace Inventory.Application.DTOs.ReturnOrder
{
    public class ReturnOrderResponseDto
    {
        public int Id { get; set; }
        public int OriginalOrderId { get; set; }
        public string CashierId { get; set; } = string.Empty;
        public DateTime ReturnDate { get; set; }
        public string? Reason { get; set; }
        public decimal TotalRefundAmount { get; set; }
        public List<ReturnOrderItemResponseDto> Items { get; set; } = new();
    }
}
