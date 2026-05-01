namespace Inventory.Domain.Enums
{
    public enum OrderStatus
    {
        /// <summary>Cashier is actively building the order; items can be added/removed.</summary>
        Draft,

        /// <summary>Legacy status kept for backward compatibility with the old Submit workflow.</summary>
        Pending,

        /// <summary>Order is confirmed, stock has been permanently deducted.</summary>
        Completed,

        /// <summary>Order was cancelled before confirmation.</summary>
        Cancelled
    }
}
