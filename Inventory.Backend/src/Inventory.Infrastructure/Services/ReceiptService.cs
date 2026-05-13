using Inventory.Application.Interfaces.Services;
using Inventory.Domain.Exceptions;
using Inventory.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Inventory.Infrastructure.Data.Settings;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Inventory.Infrastructure.Services;

public class ReceiptService : IReceiptService
{
    private readonly ApplicationDbContext _context;
    private readonly string _systemName;

    public ReceiptService(ApplicationDbContext context, IOptions<SystemSettings> systemSettings)
    {
        _context = context;
        _systemName = systemSettings.Value.SystemName;
    }

    public async Task<byte[]> GenerateReceiptAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Cashier)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
            throw new OrderNotFoundException(orderId);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Element(ComposeHeader);
                page.Content().Element(x => ComposeContent(x, order));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(_systemName).FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().Text("Professional Receipt").FontSize(14).FontColor(Colors.Grey.Medium);
            });
        });
    }

    private void ComposeContent(IContainer container, Inventory.Domain.Entities.Order order)
    {
        container.PaddingVertical(1, Unit.Centimetre).Column(column =>
        {
            column.Spacing(5);

            column.Item().Row(row =>
            {
                row.RelativeItem().Text($"Receipt Number: #{order.Id}");
                row.RelativeItem().AlignRight().Text($"Date: {order.OrderDate:dd/MM/yyyy HH:mm}");
            });

            column.Item().Row(row =>
            {
                row.RelativeItem().Text($"Cashier: {(order.Cashier != null ? order.Cashier.FullName : "Unknown")}");
                row.RelativeItem().AlignRight().Text($"Payment: {order.PaymentMethod?.ToString() ?? "N/A"}");
            });

            column.Item().PaddingTop(25).Element(x => ComposeTable(x, order));

            column.Item().PaddingTop(25).Element(x => ComposeSummary(x, order));
        });
    }

    private void ComposeTable(IContainer container, Inventory.Domain.Entities.Order order)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3); // Product Name
                columns.RelativeColumn();  // Quantity
                columns.RelativeColumn();  // Unit Price
                columns.RelativeColumn();  // Total Price
            });

            table.Header(header =>
            {
                header.Cell().Element(CellStyle).Text("Product");
                header.Cell().Element(CellStyle).AlignRight().Text("Quantity");
                header.Cell().Element(CellStyle).AlignRight().Text("Unit Price");
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
                table.Cell().Element(CellStyle).AlignRight().Text($"${item.UnitPrice:0.00}");
                table.Cell().Element(CellStyle).AlignRight().Text($"${item.TotalPrice:0.00}");

                static IContainer CellStyle(IContainer container)
                {
                    return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                }
            }
        });
    }

    private void ComposeSummary(IContainer container, Inventory.Domain.Entities.Order order)
    {
        container.Row(row =>
        {
            row.RelativeItem(); // Spacer

            row.RelativeItem().Column(column =>
            {
                column.Item().Row(r => { r.RelativeItem().Text("Subtotal:"); r.RelativeItem().AlignRight().Text($"${order.SubTotal:0.00}"); });
                column.Item().Row(r => { r.RelativeItem().Text($"Discount ({order.DiscountPercentage}%):"); r.RelativeItem().AlignRight().Text($"${order.DiscountAmount:0.00}"); });
                column.Item().Row(r => { r.RelativeItem().Text("Tax:"); r.RelativeItem().AlignRight().Text($"${order.TaxAmount:0.00}"); });
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
            x.Span("Thank you for your purchase").FontSize(12).SemiBold();
        });
    }
}
