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
    public async Task<IEnumerable<ProfitMarginDto>> GetProfitMarginsAsync(
    DateTime startDate,
    DateTime endDate,
    CancellationToken cancellationToken)
    {
        var orderItems = await _dbContext.OrderItems
            .AsNoTracking()
            .Include(oi => oi.Product)
            .Include(oi => oi.Allocations)
                .ThenInclude(a => a.StockBatch)
            .Include(oi => oi.Order)
            .Where(oi =>
                oi.Order.Status == OrderStatus.Completed &&
                oi.Order.OrderDate >= startDate &&
                oi.Order.OrderDate <= endDate)
            .ToListAsync(cancellationToken);

        return orderItems
            .GroupBy(oi => new
            {
                oi.ProductId,
                oi.Product.Name
            })
            .Select(g =>
            {
                var revenue = g.Sum(x => x.TotalPrice);

                var cost = g.Sum(x =>
                    x.Allocations.Sum(a =>
                        a.QuantityTaken * a.StockBatch.UnitCost));

                var profit = revenue - cost;

                return new ProfitMarginDto
                {
                    ProductId = g.Key.ProductId,

                    ProductName = g.Key.Name,

                    Revenue = revenue,

                    Cost = cost,

                    Profit = profit,

                    ProfitMarginPercentage =
                        revenue == 0
                            ? 0
                            : Math.Round((profit / revenue) * 100m, 2)
                };
            })
            .OrderByDescending(x => x.Profit)
            .ToList();
    }

}