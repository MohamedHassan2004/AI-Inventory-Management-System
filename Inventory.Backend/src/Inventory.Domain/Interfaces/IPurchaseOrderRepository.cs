using Inventory.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Domain.Interfaces
{
    public interface IPurchaseOrderRepository : IRepository<PurchaseOrder>
    {
        Task<PurchaseOrder?> GetPurchaseOrderWithItemsAsync(int id, CancellationToken cancellationToken = default);
        Task<PurchaseOrder?> GetFullPurchaseOrderAsync(int id, CancellationToken cancellationToken = default);
    }
}
