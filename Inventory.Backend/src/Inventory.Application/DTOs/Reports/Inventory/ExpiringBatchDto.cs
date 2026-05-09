using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.DTOs.Reports.Inventory;

public class ExpiringBatchDto
{
    public int BatchId { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public DateTime ExpireDate { get; set; }

    public decimal RemainingQuantity { get; set; }

    public int DaysRemaining { get; set; }
}