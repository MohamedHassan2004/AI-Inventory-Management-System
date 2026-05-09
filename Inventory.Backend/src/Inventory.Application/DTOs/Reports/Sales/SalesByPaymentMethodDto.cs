using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.DTOs.Reports.Sales;

public class SalesByPaymentMethodDto
{
    public string PaymentMethod { get; set; } = string.Empty;

    public int TotalOrders { get; set; }

    public decimal TotalRevenue { get; set; }
}