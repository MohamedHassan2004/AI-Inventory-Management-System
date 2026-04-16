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

        /// <summary>
        /// Get full order with all related data required for business logic
        /// (items, products, batches, consumptions)
        /// </summary>
        public async Task<Order?> GetFullOrderAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.Batches)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Consumptions)
                        .ThenInclude(c => c.Batch)
                .AsSplitQuery()
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        }
    }
}