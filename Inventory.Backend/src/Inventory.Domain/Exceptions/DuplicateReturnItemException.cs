using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Domain.Exceptions
{
    public class DuplicateReturnItemException : Exception
    {
        public int OrderItemId { get; }

        public DuplicateReturnItemException(int orderItemId)
            : base($"Order item '{orderItemId}' is already added to this return.")
        {
            OrderItemId = orderItemId;
        }
    }
}