namespace Inventory.Application.DTOs.Reports.Purchases;

public class SupplierPurchasesReportItemDto
{
    public int SupplierId { get; set; }

    public string SupplierName { get; set; } = string.Empty;

    public decimal TotalSpent { get; set; }

    public int TotalProductsSupplied { get; set; }

    public double AvgRating { get; set; }

    public int TotalPurchaseOrders { get; set; }

    public decimal ReturnedQuantity { get; set; }

    public decimal ReturnRate { get; set; }
}
