using Inventory.Domain.Entities;

namespace Inventory.Domain.Interfaces
{
    public interface ISupplierRepository : IRepository<Supplier>
    {
        Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);
        Task<Supplier?> GetSupplierWithNotesAsync(int id, CancellationToken cancellationToken = default);
        Task<bool> HasRelatedStockBatchesAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Supplier>> GetDeletedSuppliersAsync(CancellationToken cancellationToken = default);
        Task<Supplier?> GetByIdWithDeletedAsync(int id, CancellationToken cancellationToken = default);
    }
}