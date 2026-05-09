using System;
using System.Collections.Generic;
using System.Text;
using Inventory.Application.DTOs.Dashboard;
using Inventory.Application.Interfaces.Services;
using Inventory.Domain.Enums;
using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _dbContext;

    public DashboardService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(
        CancellationToken cancellationToken)
    {
        var completedOrders = await _dbContext.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Completed)
            .ToListAsync(cancellationToken);

        var products = await _dbContext.Products
            .AsNoTracking()
            .Include(p => p.Batches)
            .ToListAsync(cancellationToken);

        var returnOrders = await _dbContext.ReturnOrders
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var pendingPurchaseOrders = await _dbContext.PurchaseOrders
            .AsNoTracking()
            .CountAsync(po =>
                po.Status == PurchaseOrderStatus.Pending,
                cancellationToken);

        var activeUsers = await _dbContext.Users
            .AsNoTracking()
            .CountAsync(u =>
                u.AccountStatus == AccountStatus.Active,
                cancellationToken);

        var totalStockValue = await _dbContext.StockBatches
            .AsNoTracking()
            .SumAsync(b =>
                b.RemainingQuantity * b.UnitCost,
                cancellationToken);

        return new DashboardSummaryDto
        {
            TotalRevenue = completedOrders.Sum(o => o.FinalTotal),

            TotalOrders = completedOrders.Count,

            LowStockProducts = products.Count(p =>
                p.StockQuantity > 0 &&
                p.StockQuantity <= p.ReorderPoint),

            TotalStockValue = totalStockValue,

            TotalReturns = returnOrders.Count,

            TotalRefundAmount = returnOrders.Sum(r =>
                r.TotalRefundAmount),

            PendingPurchaseOrders = pendingPurchaseOrders,

            ActiveUsers = activeUsers
        };
    }
}