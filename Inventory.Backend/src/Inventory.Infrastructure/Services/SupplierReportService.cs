using System;
using System.Collections.Generic;
using System.Text;
using Inventory.Application.DTOs.Supplier;
using Inventory.Application.Interfaces.Services;
using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

using Inventory.Domain.Shared;

namespace Inventory.Infrastructure.Services;

public class SupplierReportService : ISupplierReportService
{
    private readonly ApplicationDbContext _dbContext;

    public SupplierReportService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<SupplierReportItemDto>> GetSuppliersReportAsync(
    DateTime? startDate,
    DateTime? endDate,
    int page,
    int pageSize,
    CancellationToken cancellationToken)
    {
        var query = _dbContext.Suppliers
            .AsNoTracking()
            .Select(s => new SupplierReportItemDto
            {
                SupplierId = s.Id,
                SupplierName = s.Name,
                PhoneNumber = s.PhoneNumber,
                Address = s.Address ?? "",
                ContactInfo = s.ContactInfo ?? "",
                AvgRating = s.AvgRating,

                TotalPurchaseOrders = s.PurchaseOrders
                    .Where(po =>
                        (!startDate.HasValue || po.OrderDate >= startDate.Value) &&
                        (!endDate.HasValue || po.OrderDate <= endDate.Value))
                    .Count(),

                TotalSpent = s.PurchaseOrders
                    .Where(po =>
                        (!startDate.HasValue || po.OrderDate >= startDate.Value) &&
                        (!endDate.HasValue || po.OrderDate <= endDate.Value))
                    .Sum(po => (decimal?)po.FinalTotal) ?? 0,

                TotalProductsSupplied = s.PurchaseOrders
                    .Where(po =>
                        (!startDate.HasValue || po.OrderDate >= startDate.Value) &&
                        (!endDate.HasValue || po.OrderDate <= endDate.Value))
                    .SelectMany(po => po.Items)
                    .Sum(i => (int?)i.Quantity) ?? 0,

                ReturnedQuantity = s.StockBatches
                    .SelectMany(sb => sb.Allocations)
                    .Sum(a => (decimal?)a.ReturnedQuantity) ?? 0
            });

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.TotalSpent)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            item.ReturnRate = item.TotalProductsSupplied == 0
                ? 0 : Math.Round((item.ReturnedQuantity / item.TotalProductsSupplied) * 100m, 2);
        }

        return new PagedResult<SupplierReportItemDto>(
            items,
            page,
            pageSize,
            totalCount);
    }
}