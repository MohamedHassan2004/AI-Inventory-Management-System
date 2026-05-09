using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory.Application.DTOs.Reports.Inventory;

public class InventoryTurnoverDto
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal QuantitySold { get; set; }

    public decimal CurrentStock { get; set; }

    public decimal TurnoverRatio { get; set; }
}