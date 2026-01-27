using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Domain.Interfaces
{
    public interface IDateTimeProvider
    {
        DateTime UtcNow { get; }
    }
}
