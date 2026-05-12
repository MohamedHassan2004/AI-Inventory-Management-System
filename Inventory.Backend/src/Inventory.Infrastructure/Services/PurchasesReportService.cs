using System;
using System.Collections.Generic;
using System.Text;
using Inventory.Application.DTOs.Reports.Purchases;
using Inventory.Application.Interfaces.Services;
using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Services;

public class PurchasesReportService : IPurchasesReportService
{
    private readonly ApplicationDbContext _dbContext;

    public PurchasesReportService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PurchasesSummaryDto> GetPurchasesSummaryAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var purchaseOrders = await _dbContext.PurchaseOrders
            .AsNoTracking()
            .Where(po =>
                po.OrderDate >= startDate &&
                po.OrderDate <= endDate)
            .ToListAsync(cancellationToken);

        var statusBreakdown = purchaseOrders
            .GroupBy(po => po.Status)
            .Select(g => new PurchaseOrderStatusDto
            {
                Status = g.Key.ToString(),
                Count = g.Count()
            })
            .ToList();

        return new PurchasesSummaryDto
        {
            TotalPurchaseOrders = purchaseOrders.Count,

            TotalPurchaseCost = purchaseOrders.Sum(po => po.FinalTotal),

            StatusBreakdown = statusBreakdown
        };
    }
    public async Task<IEnumerable<TopSupplierDto>> GetTopSuppliersAsync(
    DateTime startDate,
    DateTime endDate,
    int top,
    CancellationToken cancellationToken)
    {
        var purchaseOrders = await _dbContext.PurchaseOrders
            .AsNoTracking()
            .Include(po => po.Supplier)
            .Include(po => po.Items)
            .Where(po =>
                po.OrderDate >= startDate &&
                po.OrderDate <= endDate)
            .ToListAsync(cancellationToken);

        return purchaseOrders
            .GroupBy(po => new
            {
                po.SupplierId,
                po.Supplier.Name
            })
            .Select(g => new TopSupplierDto
            {
                SupplierId = g.Key.SupplierId,

                SupplierName = g.Key.Name,

                TotalOrders = g.Count(),

                TotalSpent = g.Sum(x => x.FinalTotal),

                TotalProductsSupplied = g
                    .SelectMany(x => x.Items)
                    .Select(i => i.ProductId)
                    .Distinct()
                    .Count()
            })
            .OrderByDescending(x => x.TotalSpent)
            .Take(top)
            .ToList();
    }
    public async Task<IEnumerable<SupplierPerformanceDto>> GetSupplierPerformanceAsync(
    DateTime startDate,
    DateTime endDate,
    CancellationToken cancellationToken)
    {
        var purchaseOrders = await _dbContext.PurchaseOrders
            .AsNoTracking()
            .Include(po => po.Supplier)
            .Include(po => po.Items)
            .Where(po =>
                po.OrderDate >= startDate &&
                po.OrderDate <= endDate)
            .ToListAsync(cancellationToken);

        var returnItems = await _dbContext.ReturnOrderItems
            .AsNoTracking()
            .Include(ri => ri.OriginalOrderItem)
                .ThenInclude(oi => oi.Allocations)
                    .ThenInclude(a => a.StockBatch)
            .ToListAsync(cancellationToken);

        return purchaseOrders
            .GroupBy(po => new
            {
                po.SupplierId,
                po.Supplier.Name
            })
            .Select(g =>
            {
                var supplierId = g.Key.SupplierId;

                var totalProductsSupplied = g
                    .SelectMany(x => x.Items)
                    .Sum(i => i.Quantity);

                var returnedQuantity = returnItems
                    .SelectMany(ri =>
                        ri.OriginalOrderItem.Allocations
                            .Where(a =>
                                a.StockBatch.SupplierId == supplierId)
                            .Select(a => a.ReturnedQuantity))
                    .Sum();

                return new SupplierPerformanceDto
                {
                    SupplierId = supplierId,

                    SupplierName = g.Key.Name,

                    TotalPurchaseOrders = g.Count(),

                    TotalSpent = g.Sum(x => x.FinalTotal),

                    TotalProductsSupplied = totalProductsSupplied,

                    ReturnedQuantity = returnedQuantity,

                    ReturnRate =
                        totalProductsSupplied == 0
                            ? 0
                            : Math.Round(
                                (returnedQuantity / totalProductsSupplied) * 100m,
                                2)
                };
            })
            .OrderByDescending(x => x.TotalSpent)
            .ToList();
    }
}