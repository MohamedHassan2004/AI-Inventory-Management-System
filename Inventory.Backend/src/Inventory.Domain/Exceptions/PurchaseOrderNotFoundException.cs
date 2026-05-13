using System;

namespace Inventory.Domain.Exceptions
{
    public class PurchaseOrderNotFoundException : Exception
    {
        public PurchaseOrderNotFoundException(int purchaseOrderId)
            : base($"Purchase order with ID {purchaseOrderId} was not found.")
        {
        }
    }
}
