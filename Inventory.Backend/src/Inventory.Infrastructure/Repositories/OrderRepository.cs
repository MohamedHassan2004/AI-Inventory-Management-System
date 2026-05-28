using Inventory.Domain.Entities;
using Inventory.Domain.Interfaces;
using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Repositories
{
    public class OrderRepository : Repository<Order>, IOrderRepository
    {
        private readonly ApplicationDbContext _context;

        public OrderRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Order?> GetOrderWithItemsAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        }

        public async Task<Order?> GetFullOrderAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.Batches)

                .Include(o => o.Items)
                    .ThenInclude(i => i.Allocations)
                        .ThenInclude(a => a.StockBatch)

                .AsSplitQuery()
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        }

        public async Task<Order?> GetDraftByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id && o.Status == Domain.Enums.OrderStatus.Draft, cancellationToken);
        }

        // Loads Items → Allocations → StockBatch for FailDelivery to restore exact batch quantities
        public async Task<Order?> GetOutForDeliveryByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Allocations)
                        .ThenInclude(a => a.StockBatch)
                .AsSplitQuery()
                .FirstOrDefaultAsync(o => o.Id == id && o.Status == Domain.Enums.OrderStatus.OutForDelivery, cancellationToken);
        }

        public async Task<IReadOnlyList<Order>> GetExpiredDraftsAsync(DateTime olderThan, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .Where(o => o.Status == Domain.Enums.OrderStatus.Draft && o.ExpiresAt < olderThan)
                .ToListAsync(cancellationToken);
        }
    }
}