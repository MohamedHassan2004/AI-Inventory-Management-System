using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.DTOs.Reports.Inventory;

public class InventoryStockSummaryDto
{
    public int TotalProducts { get; set; }

    public decimal TotalStockQuantity { get; set; }

    public int LowStockProducts { get; set; }

    public int OutOfStockProducts { get; set; }
}