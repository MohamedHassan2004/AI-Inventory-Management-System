using Inventory.Domain.Enums;

namespace Inventory.Domain.Exceptions
{
    public class OrderNotEditableException : Exception
    {
        public OrderStatus Status { get; }

        public OrderNotEditableException(OrderStatus status)
            : base($"Cannot modify or operate on an order with status '{status}'. Order must be 'Pending'.")
        {
            Status = status;
        }
    }
}
