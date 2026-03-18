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
    public class StockBatchRepository : Repository<StockBatch>, IStockBatchRepository
    {
        private readonly ApplicationDbContext _context;

        public StockBatchRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<StockBatch>> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default)
        {
            return await _context.StockBatches
                .Include(sb => sb.Supplier)
                .Include(sb => sb.Product)
                .Where(sb => sb.ProductId == productId)
                .ToListAsync(cancellationToken);
        }

        public async Task<StockBatch?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.StockBatches
                .Include(sb => sb.Supplier)
                .Include(sb => sb.Product)
                .FirstOrDefaultAsync(sb => sb.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<StockBatch>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.StockBatches
                .Include(sb => sb.Supplier)
                .Include(sb => sb.Product)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<StockBatch>> GetExpiringBatchesAsync(DateTime thresholdDate, CancellationToken cancellationToken = default)
        {
            return await _context.StockBatches
                .Include(sb => sb.Supplier)
                .Include(sb => sb.Product)
                .Where(sb => sb.ExpireDate <= thresholdDate)
                .OrderBy(sb => sb.ExpireDate)
                .ToListAsync(cancellationToken);
        }
    }
}
