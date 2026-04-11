using System;
using System.Collections.Generic;
using System.Text;

using Inventory.Domain.Enums;

namespace Inventory.Application.DTOs.Order
{
    public class CompleteOrderDto
    {
        public PaymentMethod PaymentMethod { get; set; }
        public OrderType OrderType { get; set; }
    }
}
