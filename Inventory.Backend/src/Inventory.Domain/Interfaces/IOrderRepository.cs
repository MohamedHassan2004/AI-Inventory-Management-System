using Inventory.Domain.Entities;

namespace Inventory.Domain.Interfaces
{
    public interface IOrderRepository : IRepository<Order>
    {
        Task<Order?> GetOrderWithItemsAsync(int id, CancellationToken cancellationToken = default);
        Task<Order?> GetFullOrderAsync(int id, CancellationToken cancellationToken = default);
    }
}