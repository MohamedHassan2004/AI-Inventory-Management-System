using System;
using System.Collections.Generic;
using System.Text;
using Inventory.Domain.Entities;
using Inventory.Domain.Interfaces;
using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Repositories
{
    public class ReturnOrderRepository : Repository<ReturnOrder>, IReturnOrderRepository
    {
        private readonly ApplicationDbContext _context;

        public ReturnOrderRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────────
        // Get return order with items only
        // ─────────────────────────────────────────────
        public async Task<ReturnOrder?> GetWithItemsAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.ReturnOrders
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        }

        // ─────────────────────────────────────────────
        // Get full return order with all relations
        // ─────────────────────────────────────────────
        public async Task<ReturnOrder?> GetFullAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.ReturnOrders
                .Include(r => r.Items)
                    .ThenInclude(i => i.Product)
                .Include(r => r.Items)
                    .ThenInclude(i => i.OriginalOrderItem)
                .Include(r => r.OriginalOrder)
                .AsSplitQuery()
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        }
    }
}