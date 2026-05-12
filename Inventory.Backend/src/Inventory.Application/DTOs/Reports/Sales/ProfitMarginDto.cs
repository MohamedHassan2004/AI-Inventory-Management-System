using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.DTOs.Reports.Sales;

public class ProfitMarginDto
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal Revenue { get; set; }

    public decimal Cost { get; set; }

    public decimal Profit { get; set; }

    public decimal ProfitMarginPercentage { get; set; }
}