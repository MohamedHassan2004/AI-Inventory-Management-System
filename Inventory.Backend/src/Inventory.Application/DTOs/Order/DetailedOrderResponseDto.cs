using System;
using System.Collections.Generic;
using System.Text;

using Inventory.Domain.Enums;

namespace Inventory.Application.DTOs.Order
{
    public class DetailedOrderResponseDto
    {
        public int Id { get; set; }

        public DateTime OrderDate { get; set; }
        public string CashierId { get; set; } = string.Empty;
        public string CashierName { get; set; } = string.Empty;
        public OrderStatus Status { get; set; }
        public OrderType Type { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }

        public decimal SubTotal { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal FinalTotal { get; set; }

        public string RowVersion { get; set; } = string.Empty;
        public List<OrderItemResponseDto> Items { get; set; } = new();
    }
}