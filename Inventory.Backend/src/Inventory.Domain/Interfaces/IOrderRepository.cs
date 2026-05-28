using Inventory.Domain.Entities;

namespace Inventory.Domain.Interfaces
{
    public interface IOrderRepository : IRepository<Order>
    {
        
        Task<Order?> GetOrderWithItemsAsync(int id, CancellationToken cancellationToken = default);

        
        Task<Order?> GetFullOrderAsync(int id, CancellationToken cancellationToken = default);

        
        Task<Order?> GetDraftByIdAsync(int id, CancellationToken cancellationToken = default);

        // Loads order with Items → Allocations → StockBatch — needed for FailDelivery stock restoration
        Task<Order?> GetOutForDeliveryByIdAsync(int id, CancellationToken cancellationToken = default);

        
        Task<IReadOnlyList<Order>> GetExpiredDraftsAsync(DateTime olderThan, CancellationToken cancellationToken = default);
    }
}