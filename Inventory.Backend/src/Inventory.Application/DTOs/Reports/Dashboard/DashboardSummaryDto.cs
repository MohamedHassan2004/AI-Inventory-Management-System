using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.DTOs.Reports.Dashboard;

public class DashboardSummaryDto
{
    public decimal TotalRevenue { get; set; }

    public int TotalOrders { get; set; }

    public int LowStockProducts { get; set; }

    public int OutOfStockProducts { get; set; }

    public decimal TotalStockValue { get; set; }

    public int TotalProducts { get; set; }

    public decimal TotalStockQuantity { get; set; }

    public int TotalReturns { get; set; }

    public decimal TotalRefundAmount { get; set; }

    public int PendingPurchaseOrders { get; set; }

    public int ActiveUsers { get; set; }
}