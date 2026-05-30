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

        public async Task<Order?> GetForDetailedResponseAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Allocations)
                .AsSplitQuery()
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        }

        public async Task<Order?> GetDraftForMutationAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.Batches)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Allocations)
                        .ThenInclude(a => a.StockBatch)
                .AsSplitQuery()
                .FirstOrDefaultAsync(o => o.Id == id && o.Status == Domain.Enums.OrderStatus.Draft, cancellationToken);
        }

        public async Task<Order?> GetDraftForConfirmationAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.Batches)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Allocations)
                        .ThenInclude(a => a.StockBatch)
                .AsSplitQuery()
                .FirstOrDefaultAsync(o => o.Id == id && o.Status == Domain.Enums.OrderStatus.Draft, cancellationToken);
        }

        public async Task<Order?> GetCompletedForReturnAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Allocations)
                        .ThenInclude(a => a.StockBatch)
                .AsSplitQuery()
                .FirstOrDefaultAsync(o => o.Id == id && o.Status == Domain.Enums.OrderStatus.Completed, cancellationToken);
        }

        public async Task<Order?> GetOutForDeliveryForStatusChangeAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.Status == Domain.Enums.OrderStatus.OutForDelivery, cancellationToken);
        }

        public async Task<Order?> GetOutForDeliveryForRestockAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Allocations)
                        .ThenInclude(a => a.StockBatch)
                .AsSplitQuery()
                .FirstOrDefaultAsync(o => o.Id == id && o.Status == Domain.Enums.OrderStatus.OutForDelivery, cancellationToken);
        }

        public async Task<IReadOnlyList<Order>> GetExpiredDraftsForCleanupAsync(DateTime olderThan, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Allocations)
                        .ThenInclude(a => a.StockBatch)
                .AsSplitQuery()
                .Where(o => o.Status == Domain.Enums.OrderStatus.Draft && o.ExpiresAt < olderThan)
                .ToListAsync(cancellationToken);
        }
    }
}
