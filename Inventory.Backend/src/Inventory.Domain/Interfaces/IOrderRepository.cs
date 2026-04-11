using System;
using System.Collections.Generic;
using System.Text;
using Inventory.Domain.Entities;

namespace Inventory.Domain.Interfaces
{
    public interface IOrderRepository : IRepository<Order>
    {
        Task<IEnumerable<Order>> GetPendingOrdersAsync(CancellationToken cancellationToken = default);

        Task<Order?> GetWithItemsAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Order>> GetOrdersByProductIdAsync(int productId, CancellationToken cancellationToken = default);
    }
}