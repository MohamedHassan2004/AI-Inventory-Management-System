using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.DTOs.Reports.Sales;

public class SalesTopProductDto
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal TotalQuantitySold { get; set; }

    public decimal TotalRevenue { get; set; }
}