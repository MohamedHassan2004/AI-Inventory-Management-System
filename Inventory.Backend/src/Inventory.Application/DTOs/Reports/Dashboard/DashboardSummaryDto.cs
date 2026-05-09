using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.DTOs.Dashboard;

public class DashboardSummaryDto
{
    public decimal TotalRevenue { get; set; }

    public int TotalOrders { get; set; }

    public int LowStockProducts { get; set; }

    public decimal TotalStockValue { get; set; }

    public int TotalReturns { get; set; }

    public decimal TotalRefundAmount { get; set; }

    public int PendingPurchaseOrders { get; set; }

    public int ActiveUsers { get; set; }
}