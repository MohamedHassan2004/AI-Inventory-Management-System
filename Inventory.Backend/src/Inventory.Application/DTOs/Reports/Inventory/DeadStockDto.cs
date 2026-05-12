using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.DTOs.Reports.Inventory;

public class DeadStockDto
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal StockQuantity { get; set; }

    public DateTime? LastSoldDate { get; set; }

    public int? DaysSinceLastSale { get; set; }
    
    public IEnumerable<DeadStockBatchDto> Batches { get; set; } = Enumerable.Empty<DeadStockBatchDto>();
}

public class DeadStockBatchDto
{
    public int BatchId { get; set; }
    public decimal RemainingQuantity { get; set; }
    public DateTime ExpireDate { get; set; }
}