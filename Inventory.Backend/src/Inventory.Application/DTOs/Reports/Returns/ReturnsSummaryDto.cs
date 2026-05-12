using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.DTOs.Reports.Returns;

public class ReturnsSummaryDto
{
    public int TotalReturns { get; set; }

    public decimal TotalReturnedQuantity { get; set; }

    public decimal TotalRefundAmount { get; set; }
}