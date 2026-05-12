using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Inventory.Application.DTOs.Reports.Sales;

public class SalesAnalyticsDto
{
    public IEnumerable<SalesByPaymentMethodDto> SalesByPaymentMethod { get; set; }
        = Enumerable.Empty<SalesByPaymentMethodDto>();

    public IEnumerable<PeakHourDto> PeakHours { get; set; }
        = Enumerable.Empty<PeakHourDto>();

    public IEnumerable<SalesByOrderTypeDto> SalesByOrderType { get; set; }
        = Enumerable.Empty<SalesByOrderTypeDto>();
}

public class SalesByOrderTypeDto
{
    public string OrderType { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
}