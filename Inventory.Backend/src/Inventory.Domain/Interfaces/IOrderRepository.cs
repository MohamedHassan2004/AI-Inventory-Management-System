using Inventory.Domain.Entities;

namespace Inventory.Domain.Interfaces
{
    public interface IOrderRepository : IRepository<Order>
    {
        Task<Order?> GetForDetailedResponseAsync(int id, CancellationToken cancellationToken = default);

        Task<Order?> GetDraftForMutationAsync(int id, CancellationToken cancellationToken = default);

        Task<Order?> GetDraftForConfirmationAsync(int id, CancellationToken cancellationToken = default);

        Task<Order?> GetCompletedForReturnAsync(int id, CancellationToken cancellationToken = default);

        Task<Order?> GetOutForDeliveryForStatusChangeAsync(int id, CancellationToken cancellationToken = default);

        Task<Order?> GetOutForDeliveryForRestockAsync(int id, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Order>> GetExpiredDraftsForCleanupAsync(DateTime olderThan, CancellationToken cancellationToken = default);
    }
}
