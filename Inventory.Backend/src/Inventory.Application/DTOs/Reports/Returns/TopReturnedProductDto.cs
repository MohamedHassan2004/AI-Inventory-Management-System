    using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.DTOs.Reports.Returns;

public class TopReturnedProductDto
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal TotalReturnedQuantity { get; set; }

    public decimal TotalRefundAmount { get; set; }
}