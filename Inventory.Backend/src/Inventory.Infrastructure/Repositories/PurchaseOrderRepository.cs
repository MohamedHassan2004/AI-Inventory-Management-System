using Inventory.Domain.Entities;
using Inventory.Domain.Interfaces;
using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Infrastructure.Repositories
{
    public class PurchaseOrderRepository : Repository<PurchaseOrder>, IPurchaseOrderRepository
    {
        private readonly ApplicationDbContext _context;

        public PurchaseOrderRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<PurchaseOrder?> GetPurchaseOrderWithItemsAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.PurchaseOrders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        }

        public async Task<PurchaseOrder?> GetFullPurchaseOrderAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.PurchaseOrders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .AsSplitQuery()
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        }
    }
}
