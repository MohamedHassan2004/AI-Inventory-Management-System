using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Domain.Exceptions
{
    public class EmptyReturnOrderException : Exception
    {
        public EmptyReturnOrderException()
            : base("Cannot complete return order without items.")
        {
        }
    }
}