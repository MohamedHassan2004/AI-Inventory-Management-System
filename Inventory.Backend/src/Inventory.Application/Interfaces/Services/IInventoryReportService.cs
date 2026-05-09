using Inventory.Application.DTOs.Reports.Inventory;
using System;
using System.Collections.Generic;
using System.Text;
using Inventory.Application.DTOs.Reports;


namespace Inventory.Application.Interfaces.Services;

public interface IInventoryReportService
{
    Task<StockValueDto> GetStockValueAsync(
        CancellationToken cancellationToken);
    Task<IEnumerable<ExpiringBatchDto>> GetExpiringBatchesAsync(
    int days,
    CancellationToken cancellationToken);
    Task<IEnumerable<DeadStockDto>> GetDeadStockProductsAsync(
    int days,
    CancellationToken cancellationToken);
    Task<AdvancedInventorySummaryDto> GetAdvancedSummaryAsync(
    int expiryDays,
    int deadStockDays,
    CancellationToken cancellationToken);
    Task<InventoryStockSummaryDto> GetStockSummaryAsync(
    CancellationToken cancellationToken);
    Task<IEnumerable<LowStockProductDto>> GetLowStockProductsAsync(
    CancellationToken cancellationToken);
    Task<IEnumerable<InventoryTurnoverDto>> GetInventoryTurnoverAsync(
    DateTime startDate,
    DateTime endDate,
    CancellationToken cancellationToken);
}