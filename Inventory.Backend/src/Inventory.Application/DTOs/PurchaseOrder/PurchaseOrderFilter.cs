using System;
using Inventory.Domain.Enums;

namespace Inventory.Application.DTOs.PurchaseOrder
{
    public class PurchaseOrderFilter
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public PurchaseOrderStatus? Status { get; set; }
        public int? SupplierId { get; set; }
        public int? ProductId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public decimal? MinTotal { get; set; }
        public decimal? MaxTotal { get; set; }

        public PurchaseOrderSortBy SortBy { get; set; } = PurchaseOrderSortBy.OrderDate;
        public bool SortDescending { get; set; } = true;
    }

    public enum PurchaseOrderSortBy
    {
        OrderDate,
        FinalTotal,
        Status
    }
}
