using System;
using System.Collections.Generic;
using Inventory.Domain.Enums;

namespace Inventory.Application.DTOs.PurchaseOrder
{
    public class PurchaseOrderResponseDto
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public PurchaseOrderStatus Status { get; set; }
        public decimal FinalTotal { get; set; }

        public List<PurchaseOrderItemResponseDto> Items { get; set; } = new();
    }
}
