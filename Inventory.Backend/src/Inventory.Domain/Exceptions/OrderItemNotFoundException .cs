using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Domain.Exceptions
{
    public class OrderItemNotFoundException : Exception
    {
        public int ItemId { get; }

        public OrderItemNotFoundException(int itemId)
            : base($"Order item with Id '{itemId}' was not found in this order.")
        {
            ItemId = itemId;
        }
    }
}