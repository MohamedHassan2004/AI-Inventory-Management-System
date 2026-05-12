using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.DTOs.Reports.Inventory;

public class LowStockProductDto
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal CurrentQuantity { get; set; }

    public int ReorderPoint { get; set; }
}