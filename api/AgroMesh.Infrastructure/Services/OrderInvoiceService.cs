using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AgroMesh.Infrastructure.Services;

/// <summary>
/// Renders a purchase-order invoice PDF via QuestPDF. QuestPDF.Settings.License is set once at
/// startup (Program.cs) — this class assumes that has already happened (it throws at render time
/// otherwise) rather than setting it itself, since setting a global static from a per-request
/// service would be surprising/redundant.
///
/// Money is rendered with a literal "₦" prefix + thousands separators, matching the rest of this
/// app's money-formatting convention (no currency field anywhere in the domain, see
/// TransactionRow.vue/WorkerRow.vue on the frontend) — this is the backend-side equivalent for a
/// server-rendered document.
/// </summary>
public class OrderInvoiceService : IOrderInvoiceService
{
    public byte[] Generate(OrderDto order, Farm farm)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("PURCHASE ORDER INVOICE").FontSize(18).Bold();
                    col.Item().Text(farm.Name).FontSize(12).SemiBold();
                    col.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Supplier").FontSize(9).FontColor(Colors.Grey.Darken1);
                            c.Item().Text(order.SupplierName).SemiBold();
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Order").FontSize(9).FontColor(Colors.Grey.Darken1);
                            c.Item().Text($"#{order.Id}");
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Status").FontSize(9).FontColor(Colors.Grey.Darken1);
                            c.Item().Text(order.Status).SemiBold();
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Date").FontSize(9).FontColor(Colors.Grey.Darken1);
                            c.Item().Text(order.CreatedAt.ToString("dd MMM yyyy"));
                        });
                    });
                });

                page.Content().PaddingTop(20).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(4);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Item").Bold();
                        header.Cell().AlignRight().Text("Quantity").Bold();
                        header.Cell().AlignRight().Text("Unit Price").Bold();
                        header.Cell().AlignRight().Text("Line Total").Bold();
                        header.Cell().ColumnSpan(4).PaddingTop(4).BorderBottom(1).BorderColor(Colors.Grey.Darken1);
                    });

                    foreach (var line in order.LineItems)
                    {
                        table.Cell().PaddingVertical(4).Text($"{line.InventoryItemName} ({line.Unit})");
                        table.Cell().PaddingVertical(4).AlignRight().Text($"{line.Quantity:N2}");
                        table.Cell().PaddingVertical(4).AlignRight().Text($"₦{line.UnitPrice:N2}");
                        table.Cell().PaddingVertical(4).AlignRight().Text($"₦{line.LineTotal:N2}");
                    }
                });

                page.Footer().AlignRight().PaddingTop(12).Text(text =>
                {
                    text.Span("Order Total: ").Bold();
                    text.Span($"₦{order.Total:N2}").Bold().FontSize(13);
                });
            });
        });

        return document.GeneratePdf();
    }
}
