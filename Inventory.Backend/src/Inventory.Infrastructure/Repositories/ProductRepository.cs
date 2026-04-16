using Inventory.Domain.Entities;
using Inventory.Domain.Interfaces;
using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Infrastructure.Repositories
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default)
        {
            return await _context.Products
                .Where(p => p.Name == name)
                .Where(p => !excludeId.HasValue || p.Id != excludeId.Value)
                .AnyAsync(cancellationToken);
        }

        public async Task<bool> ExistsBySkuAsync(string sku, int? excludeId = null, CancellationToken cancellationToken = default)
        {
            return await _context.Products
                .Where(p => p.SKU == sku)
                .Where(p => !excludeId.HasValue || p.Id != excludeId.Value)
                .AnyAsync(cancellationToken);
        }

        public async Task<Product?> GetWithBatchesAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Products
                .Include(p => p.Batches)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<Product>> GetWithBatchesListAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
        {
            return await _context.Products
                .Include(p => p.Batches)
                .Include(p => p.Category)
                .Where(p => ids.Contains(p.Id))
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Product>> GetAllWithBatchesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Products
                .Include(p => p.Batches)
                .Include(p => p.Category)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Product>> GetLowStockProductsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Products
                .Include(p => p.Batches)
                .Include(p => p.Category)
                .Where(p => p.Batches.Where(b => b.RemainingQuantity > 0).Sum(b => b.RemainingQuantity) <= p.ReorderPoint)
                .ToListAsync(cancellationToken);
        }
    }
}
