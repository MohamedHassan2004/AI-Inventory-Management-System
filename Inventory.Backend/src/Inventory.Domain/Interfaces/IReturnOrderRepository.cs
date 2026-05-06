using System;
using System.Collections.Generic;
using System.Text;
using Inventory.Domain.Entities;

namespace Inventory.Domain.Interfaces
{
    public interface IReturnOrderRepository : IRepository<ReturnOrder>
    {
        // Get return order with items
        Task<ReturnOrder?> GetWithItemsAsync(int id, CancellationToken cancellationToken = default);

        // Get full return order with everything (items + product + original order item)
        Task<ReturnOrder?> GetFullAsync(int id, CancellationToken cancellationToken = default);
    }
}