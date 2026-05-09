using System;
using System.Collections.Generic;
using System.Text;

using Inventory.Application.DTOs.Reports.Returns;
using Inventory.Application.Interfaces.Services;
using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Services;

public class ReturnsReportService : IReturnsReportService
{
    private readonly ApplicationDbContext _dbContext;

    public ReturnsReportService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }


    public async Task<ReturnsSummaryDto> GetReturnsSummaryAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var returnOrders = await _dbContext.ReturnOrders
            .AsNoTracking()
            .Include(r => r.Items)
            .Where(r =>
                r.ReturnDate >= startDate &&
                r.ReturnDate <= endDate)
            .ToListAsync(cancellationToken);

        var returnItems = returnOrders
            .SelectMany(r => r.Items)
            .ToList();

        return new ReturnsSummaryDto
        {
            TotalReturns = returnOrders.Count,

            TotalReturnedQuantity = returnItems.Sum(x => x.Quantity),

            TotalRefundAmount = returnOrders.Sum(x => x.TotalRefundAmount)
        };
    }
    public async Task<IEnumerable<TopReturnedProductDto>> GetTopReturnedProductsAsync(
        DateTime startDate,
        DateTime endDate,
        int top,
        CancellationToken cancellationToken)
    {
        var items = await _dbContext.ReturnOrderItems
            .AsNoTracking()
            .Where(ri =>
                ri.OriginalOrderItem.Order.OrderDate >= startDate &&
                ri.OriginalOrderItem.Order.OrderDate <= endDate)
            .Select(ri => new
            {
                ri.ProductId,
                ProductName = ri.Product.Name,
                ri.Quantity,
                RefundAmount = ri.RefundAmount
            })
            .ToListAsync(cancellationToken);

        return items
            .GroupBy(x => new
            {
                x.ProductId,
                x.ProductName
            })
            .Select(g => new TopReturnedProductDto
            {
                ProductId = g.Key.ProductId,

                ProductName = g.Key.ProductName,

                TotalReturnedQuantity = g.Sum(x => x.Quantity),

                TotalRefundAmount = g.Sum(x => x.RefundAmount)
            })
            .OrderByDescending(x => x.TotalReturnedQuantity)
            .Take(top)
            .ToList();
    }
}