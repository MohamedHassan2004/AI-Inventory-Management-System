using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.DTOs.Reports.Purchases;

public class PurchasesSummaryDto
{
    public int TotalPurchaseOrders { get; set; }

    public decimal TotalPurchaseCost { get; set; }

    public IEnumerable<PurchaseOrderStatusDto> StatusBreakdown { get; set; }
        = Enumerable.Empty<PurchaseOrderStatusDto>();
}