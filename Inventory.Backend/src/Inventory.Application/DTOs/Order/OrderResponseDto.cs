using System;
using System.Collections.Generic;
using System.Text;

using Inventory.Domain.Enums;

namespace Inventory.Application.DTOs.Order
{
    public class OrderResponseDto
    {
        public int Id { get; set; }

        public DateTime OrderDate { get; set; }
        public string CashierId { get; set; } = string.Empty;

        public OrderStatus Status { get; set; }

        public OrderType Type { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }

        public decimal SubTotal { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal FinalTotal { get; set; }

        public List<OrderItemResponseDto> Items { get; set; } = new();
    }
}