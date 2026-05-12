using Inventory.Application.DTOs.Reports.Inventory;
using System;
using System.Collections.Generic;
using System.Text;
using Inventory.Application.DTOs.Reports;
using Inventory.Domain.Shared;

namespace Inventory.Application.Interfaces.Services;

public interface IInventoryReportService
{
    Task<IEnumerable<ExpiringBatchDto>> GetExpiringBatchesAsync(
    int days,
    CancellationToken cancellationToken);
    Task<IEnumerable<DeadStockDto>> GetDeadStockProductsAsync(
    int days,
    CancellationToken cancellationToken);
    Task<IEnumerable<LowStockProductDto>> GetLowStockProductsAsync(
    CancellationToken cancellationToken);
    Task<IEnumerable<LowStockProductDto>> GetOutOfStockProductsAsync(
    CancellationToken cancellationToken);
    Task<PagedResult<InventoryTurnoverDto>> GetInventoryTurnoverAsync(
    DateTime startDate,
    DateTime endDate,
    int page,
    int pageSize,
    CancellationToken cancellationToken);
}