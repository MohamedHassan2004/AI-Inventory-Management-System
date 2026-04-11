using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
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

        public async Task<Order?> GetWithItemsAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.Batches)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Consumptions)
                        .ThenInclude(c => c.Batch)
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        }
        public async Task<IEnumerable<Order>> GetPendingOrdersAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .AsNoTracking()
                .Where(o => o.Status == OrderStatus.Pending)
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Order>> GetOrdersByProductIdAsync(int productId, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
                .AsNoTracking()
                .Where(o => o.Items.Any(i => i.ProductId == productId)) // 👈 فلتر الأول
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .ToListAsync(cancellationToken);
        }
    }
}
