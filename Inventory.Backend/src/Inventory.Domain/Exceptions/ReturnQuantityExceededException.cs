using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Domain.Exceptions
{
    public class ReturnQuantityExceededException : Exception
    {
        public int OrderItemId { get; }
        public decimal Requested { get; }
        public decimal Allowed { get; }

        public ReturnQuantityExceededException(int orderItemId, decimal requested, decimal allowed)
            : base($"Returned quantity ({requested}) exceeds allowed ({allowed}) for order item '{orderItemId}'.")
        {
            OrderItemId = orderItemId;
            Requested = requested;
            Allowed = allowed;
        }
    }
}