using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.DTOs.Reports.Purchases;

public class TopSupplierDto
{
    public int SupplierId { get; set; }

    public string SupplierName { get; set; } = string.Empty;

    public int TotalOrders { get; set; }

    public decimal TotalSpent { get; set; }

    public int TotalProductsSupplied { get; set; }
}