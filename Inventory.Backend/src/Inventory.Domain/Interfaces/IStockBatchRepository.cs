using Inventory.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Domain.Interfaces
{
    public interface IStockBatchRepository : IRepository<StockBatch>
    {
        Task<IEnumerable<StockBatch>> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default);
        Task<StockBatch?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<StockBatch>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<StockBatch>> GetBySupplierIdAsync(int supplierId, CancellationToken cancellationToken = default);
        Task<IEnumerable<StockBatch>> GetExpiringBatchesAsync(DateTime thresholdDate, CancellationToken cancellationToken = default);
    }
}
