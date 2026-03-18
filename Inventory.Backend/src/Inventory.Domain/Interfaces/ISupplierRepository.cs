using Inventory.Domain.Entities;

namespace Inventory.Domain.Interfaces
{
    public interface ISupplierRepository : IRepository<Supplier>
    {
        Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);
        Task<Supplier?> GetSupplierWithNotesAsync(int id, CancellationToken cancellationToken = default);
    }
}