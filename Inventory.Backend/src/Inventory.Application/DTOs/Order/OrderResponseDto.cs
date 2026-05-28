using Inventory.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.DTOs.Order
{
    public class OrderResponseDto
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public string CashierId { get; set; } = string.Empty;
        public string CashierName { get; set; } = string.Empty;
        public OrderStatus Status { get; set; }
        public OrderType Type { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public decimal FinalTotal { get; set; }
        public string RowVersion { get; set; } = string.Empty;
    }
}
