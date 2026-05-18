using Inventory.Application.Interfaces.Services;
using Inventory.Domain.Exceptions;
using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Inventory.Infrastructure.Data.Settings;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Inventory.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.Services;

public class PurchaseInvoiceService : IPurchaseInvoiceService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PurchaseInvoiceService> _logger;
    private readonly string _systemName;

    public PurchaseInvoiceService(ApplicationDbContext context, IOptions<SystemSettings> systemSettings, ILogger<PurchaseInvoiceService> logger)
    {
        _context = context;
        _logger = logger;
        _systemName = systemSettings.Value.SystemName;
    }

    public async Task<Result<byte[]>> GenerateInvoiceAsync(int purchaseOrderId, CancellationToken cancellationToken = default)
    {
        var purchaseOrder = await _context.PurchaseOrders
            .AsNoTracking()
            .Include(po => po.Supplier)
            .Include(po => po.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(po => po.Id == purchaseOrderId, cancellationToken);

        if (purchaseOrder == null)
        {
            _logger.LogWarning("Purchase order with ID {PurchaseOrderId} not found for invoice generation.", purchaseOrderId);
            return Result.Failure<byte[]>(new Error("NOT_FOUND", $"purchase order with ID {purchaseOrderId} not found", ErrorType.NotFound));
        }

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Element(ComposeHeader);
                page.Content().Element(x => ComposeContent(x, purchaseOrder));
                page.Footer().Element(ComposeFooter);
            });
        });

        return Result.Success(document.GeneratePdf());
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(_systemName).FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().Text("Purchase Invoice").FontSize(14).FontColor(Colors.Grey.Medium);
            });
        });
    }

    private void ComposeContent(IContainer container, Inventory.Domain.Entities.PurchaseOrder order)
    {
        container.PaddingVertical(1, Unit.Centimetre).Column(column =>
        {
            column.Spacing(5);

            column.Item().Row(row =>
            {
                row.RelativeItem().Text($"Invoice Number: #{order.Id}");
                row.RelativeItem().AlignRight().Text($"Date: {order.OrderDate:dd/MM/yyyy HH:mm}");
            });

            column.Item().PaddingTop(15).Element(x => ComposeSupplierDetails(x, order.Supplier));

            column.Item().PaddingTop(25).Element(x => ComposeTable(x, order));

            column.Item().PaddingTop(25).Element(x => ComposeSummary(x, order));
        });
    }

    private void ComposeSupplierDetails(IContainer container, Inventory.Domain.Entities.Supplier supplier)
    {
        container.Background(Colors.Grey.Lighten4).Padding(10).Column(column =>
        {
            column.Item().PaddingBottom(5).Text("Supplier Information").SemiBold().FontSize(12);
            column.Item().Text($"Name: {supplier.Name}");
            column.Item().Text($"Phone: {supplier.PhoneNumber}");
            column.Item().Text($"Address: {supplier.Address ?? "N/A"}");
            column.Item().Text($"Contact Info: {supplier.ContactInfo ?? "N/A"}");
        });
    }

    private void ComposeTable(IContainer container, Inventory.Domain.Entities.PurchaseOrder order)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3); // Product Name
                columns.RelativeColumn();  // Quantity
                columns.RelativeColumn();  // Unit Cost
                columns.RelativeColumn();  // Expiry Date
                columns.RelativeColumn();  // Total Cost
            });

            table.Header(header =>
            {
                header.Cell().Element(CellStyle).Text("Product");
                header.Cell().Element(CellStyle).AlignRight().Text("Quantity");
                header.Cell().Element(CellStyle).AlignRight().Text("Unit Cost");
                header.Cell().Element(CellStyle).AlignRight().Text("Expiry Date");
                header.Cell().Element(CellStyle).AlignRight().Text("Total");

                static IContainer CellStyle(IContainer container)
                {
                    return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                }
            });

            foreach (var item in order.Items)
            {
                table.Cell().Element(CellStyle).Text(item.Product?.Name ?? "Unknown");
                table.Cell().Element(CellStyle).AlignRight().Text($"{item.Quantity:0.##}");
                table.Cell().Element(CellStyle).AlignRight().Text($"${item.UnitCost:0.00}");
                
                string expiryText = (item.ExpiryDate == DateTime.MinValue || item.ExpiryDate == DateTime.MaxValue) 
                    ? "N/A" 
                    : item.ExpiryDate.ToString("dd/MM/yyyy");
                
                table.Cell().Element(CellStyle).AlignRight().Text(expiryText);
                table.Cell().Element(CellStyle).AlignRight().Text($"${item.TotalPrice:0.00}");

                static IContainer CellStyle(IContainer container)
                {
                    return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                }
            }
        });
    }

    private void ComposeSummary(IContainer container, Inventory.Domain.Entities.PurchaseOrder order)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("Notes:").SemiBold();
                column.Item().Text("No additional notes provided.").FontColor(Colors.Grey.Medium);
            });

            row.RelativeItem().Column(column =>
            {
                // PurchaseOrder entity only has FinalTotal natively, so we calculate subtotal
                var subTotal = order.Items.Sum(i => i.TotalPrice);
                column.Item().Row(r => { r.RelativeItem().Text("Subtotal:"); r.RelativeItem().AlignRight().Text($"${subTotal:0.00}"); });
                column.Item().Row(r => { r.RelativeItem().Text("Discount:"); r.RelativeItem().AlignRight().Text("$0.00"); });
                column.Item().Row(r => { r.RelativeItem().Text("Tax:"); r.RelativeItem().AlignRight().Text("$0.00"); });
                column.Item().PaddingTop(5).BorderTop(1).BorderColor(Colors.Black).PaddingTop(5).Row(r => 
                { 
                    r.RelativeItem().Text("Final Total:").SemiBold(); 
                    r.RelativeItem().AlignRight().Text($"${order.FinalTotal:0.00}").SemiBold(); 
                });
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(x =>
        {
            x.Span("Thank you for your business").FontSize(12).SemiBold();
        });
    }
}
