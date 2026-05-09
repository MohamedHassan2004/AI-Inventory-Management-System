using Inventory.Application.DTOs.Reports.Inventory;
using Inventory.Application.Interfaces.Services;
using Inventory.Domain.Enums;
using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Infrastructure.Services;

public class InventoryReportService : IInventoryReportService
{
    private readonly ApplicationDbContext _dbContext;

    public InventoryReportService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StockValueDto> GetStockValueAsync(
        CancellationToken cancellationToken)
    {
        var totalStockValue = await _dbContext.StockBatches
            .AsNoTracking()
            .Where(b => b.RemainingQuantity > 0)
            .SumAsync(
                b => b.RemainingQuantity * b.UnitCost,
                cancellationToken);

        return new StockValueDto
        {
            TotalStockValue = totalStockValue
        };
    }
    public async Task<IEnumerable<ExpiringBatchDto>> GetExpiringBatchesAsync(
    int days,
    CancellationToken cancellationToken)
    {
        var targetDate = DateTime.UtcNow.AddDays(days);

        return await _dbContext.StockBatches
            .AsNoTracking()
            .Where(b =>
                b.RemainingQuantity > 0 &&
                b.ExpireDate >= DateTime.UtcNow &&
                b.ExpireDate <= targetDate)
            .Select(b => new ExpiringBatchDto
            {
                BatchId = b.Id,
                ProductId = b.ProductId,
                ProductName = b.Product.Name,
                ExpireDate = b.ExpireDate,
                RemainingQuantity = b.RemainingQuantity,
                DaysRemaining = EF.Functions.DateDiffDay(
                    DateTime.UtcNow,
                    b.ExpireDate)
            })
            .OrderBy(x => x.DaysRemaining)
            .ToListAsync(cancellationToken);
    }
    public async Task<IEnumerable<DeadStockDto>> GetDeadStockProductsAsync(
    int days,
    CancellationToken cancellationToken)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);

        var lastSales = await _dbContext.OrderItems
            .AsNoTracking()
            .Where(oi => oi.Order.Status == OrderStatus.Completed)
            .GroupBy(oi => new
            {
                oi.ProductId,
                oi.Product.Name
            })
            .Select(g => new
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.Name,
                LastSoldDate = g.Max(x => x.Order.OrderDate)
            })
            .ToListAsync(cancellationToken);

        var deadStockProducts = await _dbContext.Products
            .AsNoTracking()
            .Where(p =>
                _dbContext.StockBatches
                    .Any(sb =>
                        sb.ProductId == p.Id &&
                        sb.RemainingQuantity > 0))
            .ToListAsync(cancellationToken);

        var result = deadStockProducts
            .Select(product =>
            {
                var saleInfo = lastSales
                    .FirstOrDefault(x => x.ProductId == product.Id);

                return new DeadStockDto
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    StockQuantity = product.StockQuantity,
                    LastSoldDate = saleInfo?.LastSoldDate,

                    DaysSinceLastSale = saleInfo == null
                        ? null
                        : (DateTime.UtcNow - saleInfo.LastSoldDate).Days
                };
            })
            .Where(x =>
                x.LastSoldDate == null ||
                x.LastSoldDate < cutoffDate)
            .OrderByDescending(x => x.DaysSinceLastSale)
            .ToList();

        return result;
    }

    public async Task<AdvancedInventorySummaryDto> GetAdvancedSummaryAsync(
    int expiryDays,
    int deadStockDays,
    CancellationToken cancellationToken)
    {
        var stockValue = await GetStockValueAsync(
            cancellationToken);

        var expiringBatches = await GetExpiringBatchesAsync(
            expiryDays,
            cancellationToken);

        var deadStockProducts = await GetDeadStockProductsAsync(
            deadStockDays,
            cancellationToken);

        return new AdvancedInventorySummaryDto
        {
            TotalStockValue = stockValue.TotalStockValue,

            ExpiringBatches = expiringBatches,

            DeadStockProducts = deadStockProducts
        };
    }
    public async Task<InventoryStockSummaryDto> GetStockSummaryAsync(
    CancellationToken cancellationToken)
    {
        var products = await _dbContext.Products
            .AsNoTracking()
            .Include(p => p.Batches)
            .ToListAsync(cancellationToken);

        return new InventoryStockSummaryDto
        {
            TotalProducts = products.Count,

            TotalStockQuantity = products.Sum(p => p.StockQuantity),

            LowStockProducts = products.Count(p =>
                p.StockQuantity > 0 &&
                p.StockQuantity <= p.ReorderPoint),

            OutOfStockProducts = products.Count(p =>
                p.StockQuantity <= 0)
        };
    }
    public async Task<IEnumerable<LowStockProductDto>> GetLowStockProductsAsync(
    CancellationToken cancellationToken)
    {
        var products = await _dbContext.Products
            .AsNoTracking()
            .Include(p => p.Batches)
            .ToListAsync(cancellationToken);

        return products
            .Where(p =>
                p.StockQuantity > 0 &&
                p.StockQuantity <= p.ReorderPoint)
            .Select(p => new LowStockProductDto
            {
                ProductId = p.Id,
                ProductName = p.Name,
                CurrentQuantity = p.StockQuantity,
                ReorderPoint = p.ReorderPoint
            })
            .OrderBy(p => p.CurrentQuantity)
            .ToList();
    }
    public async Task<IEnumerable<InventoryTurnoverDto>> GetInventoryTurnoverAsync(
    DateTime startDate,
    DateTime endDate,
    CancellationToken cancellationToken)
    {
        var products = await _dbContext.Products
            .AsNoTracking()
            .Include(p => p.Batches)
            .ToListAsync(cancellationToken);

        var soldQuantities = await _dbContext.OrderItems
            .AsNoTracking()
            .Where(oi =>
                oi.Order.Status == OrderStatus.Completed &&
                oi.Order.OrderDate >= startDate &&
                oi.Order.OrderDate <= endDate)
            .GroupBy(oi => oi.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                QuantitySold = g.Sum(x => x.Quantity)
            })
            .ToListAsync(cancellationToken);

        var result = products
            .Select(product =>
            {
                var sold = soldQuantities
                    .FirstOrDefault(x => x.ProductId == product.Id);

                var quantitySold = sold?.QuantitySold ?? 0;

                var currentStock = product.StockQuantity;

                decimal turnoverRatio = currentStock <= 0
                    ? quantitySold
                    : Math.Round(quantitySold / currentStock, 2);

                return new InventoryTurnoverDto
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    QuantitySold = quantitySold,
                    CurrentStock = currentStock,
                    TurnoverRatio = turnoverRatio
                };
            })
            .OrderByDescending(x => x.TurnoverRatio)
            .ToList();

        return result;
    }


}