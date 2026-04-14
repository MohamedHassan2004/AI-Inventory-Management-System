using System;
using System.Collections.Generic;
using System.Text;
using Inventory.Domain.Entities;

namespace Inventory.Domain.Interfaces
{
    public interface IOrderRepository : IRepository<Order>
    {
        Task<Order?> GetByIdWithItemsAsync(int id, CancellationToken cancellationToken = default);

        Task<Order?> GetByIdWithItemsAndProductsAsync(int id, CancellationToken cancellationToken = default);
    }
}