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
using Inventory.Application.Interfaces.Documents;

namespace Inventory.Infrastructure.Documents;

public class ReceiptService : IReceiptService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReceiptService> _logger;
    private readonly string _systemName;

    public ReceiptService(ApplicationDbContext context, IOptions<SystemSettings> systemSettings, ILogger<ReceiptService> logger)
    {
        _context = context;
        _logger = logger;
        _systemName = systemSettings.Value.SystemName;
    }

    public async Task<Result<byte[]>> GenerateReceiptAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Cashier)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Include(o => o.Items)
                .ThenInclude(i => i.Allocations)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
        {
            _logger.LogWarning("Order with ID {OrderId} not found.", orderId);
            return Result.Failure<byte[]>(new Error("ORDER_NOT_FOUND",$"Order with ID {orderId} not found.",ErrorType.NotFound));
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
                page.Content().Element(x => ComposeContent(x, order));
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
                column.Item().Text("Professional Receipt").FontSize(14).FontColor(Colors.Grey.Medium);
            });
        });
    }

    private void ComposeContent(IContainer container, Domain.Entities.Order order)
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

    private void ComposeTable(IContainer container, Domain.Entities.Order order)
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
                table.Cell().Element(CellStyle).Column(column =>
                {
                    column.Item().Text(item.Product?.Name ?? "Unknown").SemiBold();
                    
                    if (item.Allocations != null && item.Allocations.Any())
                    {
                        foreach (var alloc in item.Allocations)
                        {
                            var discountText = alloc.DiscountPercentage > 0 ? $" (Discount {alloc.DiscountPercentage:0.#}%)" : "";
                            var finalPrice = alloc.UnitPrice * (1 - alloc.DiscountPercentage / 100m);
                            column.Item().PaddingLeft(10).Text($"• Batch #{alloc.StockBatchId}: {alloc.QuantityTaken:0.##} x ${alloc.UnitPrice:0.00}{discountText} = ${alloc.QuantityTaken * finalPrice:0.00}")
                                .FontSize(9)
                                .FontColor(Colors.Grey.Medium);
                        }
                    }
                });

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

    private void ComposeSummary(IContainer container, Domain.Entities.Order order)
    {
        container.Row(row =>
        {
            row.RelativeItem(); // Spacer

            row.RelativeItem().Column(column =>
            {
                column.Item().Row(r => { r.RelativeItem().Text("Subtotal:"); r.RelativeItem().AlignRight().Text($"${order.SubTotal:0.00}"); });
                column.Item().Row(r => { r.RelativeItem().Text($"Discount ({order.DiscountPercentage}%):"); r.RelativeItem().AlignRight().Text($"${order.DiscountAmount:0.00}"); });
                column.Item().Row(r => { r.RelativeItem().Text("Tax:"); r.RelativeItem().AlignRight().Text($"${order.TaxAmount:0.00}"); });
                
                if (order.Type == Domain.Enums.OrderType.Delivery)
                {
                    column.Item().Row(r => { r.RelativeItem().Text("Delivery Fee:"); r.RelativeItem().AlignRight().Text($"${order.DeliveryFee:0.00}"); });
                }

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
