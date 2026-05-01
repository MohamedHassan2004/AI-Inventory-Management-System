using Inventory.Domain.Entities;

namespace Inventory.Domain.Interfaces
{
    public interface IOrderRepository : IRepository<Order>
    {
        /// <summary>Loads an order with its items (tracking, no product/batch data).</summary>
        Task<Order?> GetOrderWithItemsAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>Loads an order with items, products, and stock batches (tracking). Used by ConfirmOrder.</summary>
        Task<Order?> GetFullOrderAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads a Draft order with its items and product snapshots.
        /// Returns null if the order does not exist or is not in Draft status.
        /// </summary>
        Task<Order?> GetDraftByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns all Draft orders whose ExpiresAt is earlier than <paramref name="olderThan"/>.
        /// Used by the background cleanup service.
        /// </summary>
        Task<IReadOnlyList<Order>> GetExpiredDraftsAsync(DateTime olderThan, CancellationToken cancellationToken = default);
    }
}