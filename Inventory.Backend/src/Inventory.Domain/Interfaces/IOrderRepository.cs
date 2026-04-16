using System;
using System.Collections.Generic;
using System.Text;
using Inventory.Domain.Entities;

namespace Inventory.Domain.Interfaces
{
    public interface IOrderRepository : IRepository<Order>
    {
        Task<Order?> GetFullOrderAsync(int id, CancellationToken cancellationToken = default);
    }
}