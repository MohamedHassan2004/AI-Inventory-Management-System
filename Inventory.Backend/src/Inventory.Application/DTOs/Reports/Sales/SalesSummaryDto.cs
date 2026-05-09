using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.DTOs.Reports.Sales;

public class SalesSummaryDto
{
    public int TotalOrders { get; set; }

    public decimal TotalRevenue { get; set; }

    public decimal AverageOrderValue { get; set; }
}