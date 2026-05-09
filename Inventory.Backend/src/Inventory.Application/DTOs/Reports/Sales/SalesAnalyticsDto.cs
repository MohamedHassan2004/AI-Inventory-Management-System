using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.DTOs.Reports.Sales;

public class SalesAnalyticsDto
{
    public IEnumerable<SalesByPaymentMethodDto> SalesByPaymentMethod { get; set; }
        = Enumerable.Empty<SalesByPaymentMethodDto>();

    public IEnumerable<PeakHourDto> PeakHours { get; set; }
        = Enumerable.Empty<PeakHourDto>();
}