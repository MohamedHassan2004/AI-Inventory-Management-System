using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.DTOs.Reports.Sales;

public class PeakHourDto
{
    public int Hour { get; set; }

    public int TotalOrders { get; set; }
}