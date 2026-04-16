using Inventory.Domain.Enums;

namespace Inventory.Application.DTOs.Order
{
    public enum OrderSortBy
    {
        OrderDate,
        FinalTotal,
        Status,
        Type
    }

    public class OrderFilter
    {
        // ── Filters ────────────────────────────────────────────────

        public OrderStatus? Status { get; set; }

        public OrderType? Type { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }

        public int? ProductId { get; set; }
        public string? CashierId { get; set; }

        public DateTime? DateFrom { get; set; }

        public DateTime? DateTo { get; set; }

        public decimal? MinTotal { get; set; }

        public decimal? MaxTotal { get; set; }

        // ── Sorting ────────────────────────────────────────────────

        public OrderSortBy SortBy { get; set; } = OrderSortBy.OrderDate;

        public bool SortDescending { get; set; } = true;

        // ── Pagination ─────────────────────────────────────────────
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
