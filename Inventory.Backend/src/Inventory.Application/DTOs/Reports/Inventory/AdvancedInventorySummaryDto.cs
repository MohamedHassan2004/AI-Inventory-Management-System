using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.DTOs.Reports.Inventory;

public class AdvancedInventorySummaryDto
{
    public decimal TotalStockValue { get; set; }

    public IEnumerable<ExpiringBatchDto> ExpiringBatches { get; set; }
        = Enumerable.Empty<ExpiringBatchDto>();

    public IEnumerable<DeadStockDto> DeadStockProducts { get; set; }
        = Enumerable.Empty<DeadStockDto>();
}