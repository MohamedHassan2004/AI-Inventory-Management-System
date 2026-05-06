using System;

namespace Inventory.Domain.Exceptions
{
    public class EmptyPurchaseOrderException : Exception
    {
        public EmptyPurchaseOrderException() 
            : base("Cannot submit an empty purchase order.")
        {
        }
    }
}
