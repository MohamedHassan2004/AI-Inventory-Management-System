using Inventory.Domain.Entities;
using Inventory.Domain.Interfaces;
using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Repositories
{
    public class SupplierRepository : Repository<Supplier>, ISupplierRepository
    {
        private readonly ApplicationDbContext _context;

        public SupplierRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Supplier>().IgnoreQueryFilters().AnyAsync(x => x.Name == name, cancellationToken);
        }

        public async Task<Supplier?> GetSupplierWithNotesAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Supplier>()
                .Include(s => s.SupplierNotes)
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        }

        public async Task<bool> HasRelatedStockBatchesAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.StockBatches.AnyAsync(sb => sb.SupplierId == id, cancellationToken);
        }

        public async Task<IEnumerable<Supplier>> GetDeletedSuppliersAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Set<Supplier>()
                .IgnoreQueryFilters()
                .Where(s => s.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<Supplier?> GetByIdWithDeletedAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Supplier>()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        }
    }
}