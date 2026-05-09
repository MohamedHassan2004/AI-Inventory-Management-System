using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.DTOs.Reports.Purchases;

public class SupplierPerformanceDto
{
    public int SupplierId { get; set; }

    public string SupplierName { get; set; } = string.Empty;

    public int TotalPurchaseOrders { get; set; }

    public decimal TotalSpent { get; set; }

    public decimal TotalProductsSupplied { get; set; }

    public decimal ReturnedQuantity { get; set; }

    public decimal ReturnRate { get; set; }
}