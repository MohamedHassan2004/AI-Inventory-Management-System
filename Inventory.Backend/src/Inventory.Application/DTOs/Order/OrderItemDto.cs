using System;
using System.Collections.Generic;
using System.Text;


    namespace Inventory.Application.DTOs.Order
    {
        public class OrderItemDto
        {
            public int ProductId { get; set; }
            public decimal Quantity { get; set; }
        }
    }

