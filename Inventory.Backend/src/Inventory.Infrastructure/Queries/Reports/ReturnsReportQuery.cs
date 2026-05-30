using System;
using System.Collections.Generic;
using System.Text;
using Inventory.Application.DTOs.Reports.Returns;
using Inventory.Application.Interfaces.Queries.Reports;
using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Queries.Reports;

public class ReturnsReportQuery : IReturnsReportQuery
{
    private readonly ApplicationDbContext _dbContext;

    public ReturnsReportQuery(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
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