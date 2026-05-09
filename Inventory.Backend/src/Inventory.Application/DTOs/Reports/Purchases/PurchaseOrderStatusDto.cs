using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.DTOs.Reports.Purchases;

public class PurchaseOrderStatusDto
{
    public string Status { get; set; } = string.Empty;

    public int Count { get; set; }
}