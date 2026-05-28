namespace Inventory.Domain.Enums
{
    public enum OrderStatus
    {
        Draft,          // Being built by the cashier — no stock deducted yet
        OutForDelivery, // Delivery order dispatched — stock deducted, awaiting confirmation
        Completed,      // Order fulfilled (pickup done or delivery confirmed)
        Cancelled       // Draft discarded or delivery failed
    }
}
