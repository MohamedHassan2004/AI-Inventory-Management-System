using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.DTOs.Reports.Users;

public class CashierSalesDto
{
    public string CashierId { get; set; } = string.Empty;

    public string CashierName { get; set; } = string.Empty;

    public int TotalOrders { get; set; }

    public decimal TotalRevenue { get; set; }
}