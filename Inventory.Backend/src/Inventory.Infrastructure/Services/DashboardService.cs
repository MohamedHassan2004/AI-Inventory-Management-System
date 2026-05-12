using System;
using System.Collections.Generic;
using System.Text;
using Inventory.Application.DTOs.Reports.Dashboard;
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
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var ordersQuery = _dbContext.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Completed);
            
        if (startDate.HasValue) ordersQuery = ordersQuery.Where(o => o.OrderDate >= startDate.Value);
        if (endDate.HasValue) ordersQuery = ordersQuery.Where(o => o.OrderDate <= endDate.Value);

        var completedOrders = await ordersQuery.ToListAsync(cancellationToken);

        var products = await _dbContext.Products
            .AsNoTracking()
            .Include(p => p.Batches)
            .ToListAsync(cancellationToken);

        var returnsQuery = _dbContext.ReturnOrders
            .AsNoTracking().AsQueryable();
            
        if (startDate.HasValue) returnsQuery = returnsQuery.Where(r => r.ReturnDate >= startDate.Value);
        if (endDate.HasValue) returnsQuery = returnsQuery.Where(r => r.ReturnDate <= endDate.Value);

        var returnOrders = await returnsQuery.ToListAsync(cancellationToken);

        var pendingOrdersQuery = _dbContext.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Pending);
            
        if (startDate.HasValue) pendingOrdersQuery = pendingOrdersQuery.Where(o => o.OrderDate >= startDate.Value);
        if (endDate.HasValue) pendingOrdersQuery = pendingOrdersQuery.Where(o => o.OrderDate <= endDate.Value);

        var totalPendingOrders = await pendingOrdersQuery.CountAsync(cancellationToken);

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
            
            TotalProducts = products.Count,
            
            TotalStockQuantity = products.Sum(p => p.StockQuantity),

            LowStockProducts = products.Count(p =>
                p.StockQuantity > 0 &&
                p.StockQuantity <= p.ReorderPoint),
                
            OutOfStockProducts = products.Count(p => p.StockQuantity <= 0),

            TotalStockValue = totalStockValue,

            TotalReturns = returnOrders.Count,

            TotalRefundAmount = returnOrders.Sum(r =>
                r.TotalRefundAmount),

            TotalPendingOrders = totalPendingOrders,

            ActiveUsers = activeUsers
        };
    }
}