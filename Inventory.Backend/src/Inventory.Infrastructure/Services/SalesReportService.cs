using Inventory.Application.DTOs.Reports.Sales;
using Inventory.Application.Interfaces.Services;
using Inventory.Domain.Enums;
using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Infrastructure.Services;

public class SalesReportService : ISalesReportService
{
    private readonly ApplicationDbContext _dbContext;

    public SalesReportService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SalesSummaryDto> GetSalesSummaryAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Orders
            .AsNoTracking()
            .Where(o =>
                o.Status == OrderStatus.Completed &&
                o.OrderDate >= startDate &&
                o.OrderDate <= endDate);

        var totalOrders = await query.CountAsync(cancellationToken);

        var totalRevenue = await query
            .SumAsync(o => o.FinalTotal, cancellationToken);

        var averageOrderValue = totalOrders == 0
            ? 0
            : totalRevenue / totalOrders;

        return new SalesSummaryDto
        {
            TotalOrders = totalOrders,
            TotalRevenue = totalRevenue,
            AverageOrderValue = averageOrderValue
        };
    }
    public async Task<IEnumerable<SalesTopProductDto>> GetTopSellingProductsAsync(
    DateTime startDate,
    DateTime endDate,
    int top,
    CancellationToken cancellationToken)
    {
        return await _dbContext.OrderItems
            .AsNoTracking()
            .Where(oi =>
                oi.Order.Status == OrderStatus.Completed &&
                oi.Order.OrderDate >= startDate &&
                oi.Order.OrderDate <= endDate)
            .GroupBy(oi => new
            {
                oi.ProductId,
                oi.Product.Name
            })
            .Select(g => new SalesTopProductDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.Name,
                TotalQuantitySold = g.Sum(x => x.Quantity),
                TotalRevenue = g.Sum(x => x.Quantity * x.UnitPrice)
            })
            .OrderByDescending(x => x.TotalQuantitySold)
            .Take(top)
            .ToListAsync(cancellationToken);
    }
    public async Task<SalesAnalyticsDto> GetSalesAnalyticsAsync(
    DateTime startDate,
    DateTime endDate,
    CancellationToken cancellationToken)
    {
        var ordersQuery = _dbContext.Orders
            .AsNoTracking()
            .Where(o =>
                o.Status == OrderStatus.Completed &&
                o.OrderDate >= startDate &&
                o.OrderDate <= endDate);

        var salesByPaymentMethod = await ordersQuery
            .GroupBy(o => o.PaymentMethod)
            .Select(g => new SalesByPaymentMethodDto
            {
                PaymentMethod = g.Key.ToString(),
                TotalOrders = g.Count(),
                TotalRevenue = g.Sum(x => x.FinalTotal)
            })
            .ToListAsync(cancellationToken);

        var peakHours = await ordersQuery
            .GroupBy(o => o.OrderDate.Hour)
            .Select(g => new PeakHourDto
            {
                Hour = g.Key,
                TotalOrders = g.Count()
            })
            .OrderByDescending(x => x.TotalOrders)
            .ToListAsync(cancellationToken);

        return new SalesAnalyticsDto
        {
            SalesByPaymentMethod = salesByPaymentMethod,
            PeakHours = peakHours
        };
    }

}