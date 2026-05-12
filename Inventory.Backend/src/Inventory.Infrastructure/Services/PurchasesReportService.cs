using System;
using System.Collections.Generic;
using System.Text;
using Inventory.Application.DTOs.Reports.Purchases;
using Inventory.Application.Interfaces.Services;
using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

using Inventory.Domain.Shared;

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


    public async Task<PagedResult<SupplierPurchasesReportItemDto>> GetSuppliersReportAsync(
        DateTime startDate,
        DateTime endDate,
        int page,
        int pageSize,
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

        var query = purchaseOrders
            .GroupBy(po => new
            {
                po.SupplierId,
                Name = po.Supplier.Name,
                AvgRating = po.Supplier.AvgRating
            })
            .Select(g =>
            {
                var supplierId = g.Key.SupplierId;
                var totalProductsSupplied = g.SelectMany(x => x.Items).Sum(i => i.Quantity);
                var returnedQuantity = returnItems
                    .SelectMany(ri =>
                        ri.OriginalOrderItem.Allocations
                            .Where(a => a.StockBatch.SupplierId == supplierId)
                            .Select(a => a.ReturnedQuantity))
                    .Sum();

                var returnRate = totalProductsSupplied == 0
                    ? 0
                    : Math.Round((returnedQuantity / totalProductsSupplied) * 100m, 2);

                return new SupplierPurchasesReportItemDto
                {
                    SupplierId = supplierId,
                    SupplierName = g.Key.Name,
                    TotalSpent = g.Sum(x => x.FinalTotal),
                    TotalProductsSupplied = (int)totalProductsSupplied,
                    AvgRating = g.Key.AvgRating,
                    TotalPurchaseOrders = g.Count(),
                    ReturnedQuantity = returnedQuantity,
                    ReturnRate = returnRate
                };
            })
            .OrderByDescending(x => x.TotalSpent)
            .ToList();

        var totalCount = query.Count;
        var pagedItems = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<SupplierPurchasesReportItemDto>(pagedItems, page, pageSize, totalCount);
    }
}