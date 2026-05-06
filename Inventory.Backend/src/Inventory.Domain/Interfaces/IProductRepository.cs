using Inventory.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Domain.Interfaces
{
    public interface IProductRepository : IRepository<Product>
    {
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
        Task<bool> ExistsBySkuAsync(string sku, int? excludeId = null, CancellationToken cancellationToken = default);
        Task<Product?> GetWithBatchesAsync(int id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Product>> GetWithBatchesListAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
        Task<IEnumerable<Product>> GetAllWithBatchesAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Product>> GetLowStockProductsAsync(CancellationToken cancellationToken = default);

        
        Task<IEnumerable<Product>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
    }
}
