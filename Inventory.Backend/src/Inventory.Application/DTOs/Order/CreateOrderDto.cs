
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.DTOs.Order
{
    public class CreateOrderDto
    {
        public List<OrderItemDto> Items { get; set; } = new();
    }
}