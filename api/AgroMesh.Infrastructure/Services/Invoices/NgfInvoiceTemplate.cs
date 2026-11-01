using System.Globalization;
using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Services;
using AgroMesh.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AgroMesh.Infrastructure.Services.Invoices;

/// <summary>
/// "NGF - Invoice": the Nature's Gold Farm layout. A4 portrait; logo and INVOICE title top right, BILLING TO top left;
/// an item table whose header is a yellow tab (S/N, description) beside a green bar (rate, qty, amount); subtotal, VAT and
/// a TOTAL band; payment method and terms at the left, signature line at the right; contact rows and three decorative
/// lines in the footer. The brand colours are constants of this template, not farm settings.
/// </summary>
public class NgfInvoiceTemplate : IInvoiceTemplateRenderer
{
    public const string Green = "#138A44";
    public const string Yellow = "#F5C90C";
    public const string Ink = "#1F2A24";
    public const string Muted = "#5F6B64";
    public const string GridGrey = "#C8CCD0";
    public const string RuleGrey = "#A9AEB3";

    private const int MinimumRows = 5;
    private const float PageMargin = 40f;
    private const float RateWidth = 100f;
    private const float QtyWidth = 50f;
    private const float AmountWidth = 110f;
    private const float SerialWidth = 40f;
    private const float Slant = 14f;

    public string Key => InvoiceTemplateKeys.Ngf;
    public string Name => "NGF - Invoice";
    public string Description => "Nature's Gold Farm: logo top right, yellow and green item table, TOTAL band and contact footer.";

    public byte[] Render(InvoiceRenderModel model)
    {
        InvoiceFonts.EnsureRegistered();
        try
        {
            return Compose(model, TryLoadLogo(model.Issuer.Logo)).GeneratePdf();
        }
        catch (Exception) when (model.Issuer.Logo is not null)
        {
            // A logo that will not draw must not stop an invoice from printing.
            return Compose(model, null).GeneratePdf();
        }
    }

    // ---- helpers shared with the tests -------------------------------------------------------------------------

    public static string Money(decimal value) => "₦" + value.ToString("N2", CultureInfo.InvariantCulture);

    public static string VatLabel(decimal percent) => $"TAX VAT ({percent.ToString("0.##", CultureInfo.InvariantCulture)}%)";

    private static string F(float v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    private static string Day(DateOnly d) => d.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);

    private static Image? TryLoadLogo(byte[]? bytes)
    {
        if (bytes is null || UploadedFileValidator.Sniff(bytes) is null or UploadKind.Pdf) return null;
        try { return Image.FromBinaryData(bytes); }
        catch (Exception) { return null; }
    }

    // ---- document ------------------------------------------------------------------------------------------------

    public static Document Compose(InvoiceRenderModel model, Image? logo)
    {
        InvoiceFonts.EnsureRegistered();
        return ComposeDocument(model, logo);
    }

    private static Document ComposeDocument(InvoiceRenderModel model, Image? logo) =>
        Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(0);
            page.DefaultTextStyle(t => t.FontFamily(InvoiceFonts.Family).FontSize(9.5f).FontColor(Ink));

            page.Content().PaddingHorizontal(PageMargin).PaddingTop(34).Column(col =>
            {
                col.Item().Element(c => Header(c, model, logo));
                col.Item().PaddingTop(28).Element(c => ItemTable(c, model.Invoice));
                col.Item().PaddingTop(14).Element(c => Totals(c, model.Invoice));
                PaymentAndSignature(col, model.Invoice);
            });

            page.Footer().Element(c => Footer(c, model.Issuer));
        }));

    private static void Header(IContainer container, InvoiceRenderModel model, Image? logo)
    {
        var invoice = model.Invoice;
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("BILLING TO:").FontSize(11).Bold();
                col.Item().PaddingTop(3).Width(56).Height(3).Background(Yellow);
                col.Item().PaddingTop(10).Text(invoice.BillToName).FontSize(12).Bold();
                ContactRow(col, "P:", invoice.BillToPhone);
                ContactRow(col, "E:", invoice.BillToEmail);
                ContactRow(col, "A:", invoice.BillToAddress);
            });

            row.ConstantItem(220).Column(col =>
            {
                if (logo is not null)
                    col.Item().Height(66).AlignRight().AlignTop().Image(logo).FitArea();
                else
                    col.Item().Height(66).AlignRight().AlignMiddle().Text(model.Issuer.Name).FontSize(15).ExtraBold().FontColor(Green);

                col.Item().PaddingTop(14).AlignRight().Text("INVOICE").FontSize(26).ExtraBold().FontColor(Green);
                col.Item().PaddingTop(6).Element(c => MetaRow(c, "Invoice:", invoice.Reference));
                col.Item().PaddingTop(2).Element(c => MetaRow(c, "Date:", Day(invoice.Date)));
                col.Item().PaddingTop(2).Element(c => MetaRow(c, "Due Date:", Day(invoice.DueDate)));
            });
        });
    }

    private static void ContactRow(ColumnDescriptor col, string tag, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        col.Item().PaddingTop(3).Row(r =>
        {
            r.ConstantItem(16).Text(tag).Bold().FontColor(Green);
            r.RelativeItem().Text(value);
        });
    }

    private static void MetaRow(IContainer container, string label, string value) =>
        container.Row(r =>
        {
            r.RelativeItem().AlignRight().Text(label).Bold();
            r.ConstantItem(92).AlignRight().Text(value);
        });

    // ---- items -----------------------------------------------------------------------------------------------------

    private static void ItemTable(IContainer container, Invoice invoice)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(SerialWidth);
                c.RelativeColumn();
                c.ConstantColumn(RateWidth);
                c.ConstantColumn(QtyWidth);
                c.ConstantColumn(AmountWidth);
            });

            table.Header(h => h.Cell().ColumnSpan(5).Element(HeaderBar));

            var rows = Math.Max(MinimumRows, invoice.LineItems.Count);
            for (var i = 0; i < rows; i++)
            {
                var item = i < invoice.LineItems.Count ? invoice.LineItems[i] : null;
                table.Cell().Element(c => GridCell(c, first: true)).AlignCenter().Text(item is null ? "" : (i + 1).ToString(CultureInfo.InvariantCulture));
                table.Cell().Element(c => GridCell(c)).ShowEntire().Text(item?.Description ?? "");
                table.Cell().Element(c => GridCell(c)).AlignRight().Text(item is null ? "" : Money(item.Rate));
                table.Cell().Element(c => GridCell(c)).AlignCenter().Text(item is null ? "" : item.Quantity.ToString("0.####", CultureInfo.InvariantCulture));
                table.Cell().Element(c => GridCell(c)).AlignRight().Text(item is null ? "" : Money(item.Amount));
            }
        });
    }

    private static IContainer GridCell(IContainer c, bool first = false)
    {
        var cell = c.MinHeight(26);
        if (first) cell = cell.BorderLeft(0.6f).BorderColor(GridGrey);
        return cell.BorderBottom(0.6f).BorderRight(0.6f).BorderColor(GridGrey).PaddingVertical(7).PaddingHorizontal(8);
    }

    private static void HeaderBar(IContainer container)
    {
        var fixedRight = RateWidth + QtyWidth + AmountWidth;
        container.Layers(layers =>
        {
            layers.Layer().Svg(size =>
            {
                var w = size.Width;
                var h = size.Height;
                var split = w - fixedRight;            // where the yellow tab ends and the green bar starts
                return $"<svg xmlns='http://www.w3.org/2000/svg' width='{F(w)}' height='{F(h)}' viewBox='0 0 {F(w)} {F(h)}'>"
                     + $"<polygon points='0,0 {F(split + Slant)},0 {F(split)},{F(h)} 0,{F(h)}' fill='{Yellow}'/>"
                     + $"<polygon points='{F(split + Slant + 5)},0 {F(w)},0 {F(w)},{F(h)} {F(split + 5)},{F(h)}' fill='{Green}'/>"
                     + "</svg>";
            });

            layers.PrimaryLayer().Height(30).Row(r =>
            {
                r.ConstantItem(SerialWidth).AlignMiddle().AlignCenter().Text("S/N").Bold().FontSize(9);
                r.RelativeItem().AlignMiddle().PaddingLeft(8).Text("ITEM DESCRIPTION").Bold().FontSize(9);
                r.ConstantItem(RateWidth).AlignMiddle().PaddingRight(8).AlignRight().Text("RATE").Bold().FontSize(9).FontColor(Colors.White);
                r.ConstantItem(QtyWidth).AlignMiddle().AlignCenter().Text("QTY").Bold().FontSize(9).FontColor(Colors.White);
                r.ConstantItem(AmountWidth).AlignMiddle().PaddingRight(8).AlignRight().Text("AMOUNT").Bold().FontSize(9).FontColor(Colors.White);
            });
        });
    }

    // ---- totals ----------------------------------------------------------------------------------------------------

    private static void Totals(IContainer container, Invoice invoice)
    {
        container.AlignRight().Width(RateWidth + QtyWidth + AmountWidth + 60).Column(col =>
        {
            col.Item().Row(r =>
            {
                r.RelativeItem().Text("SUBTOTAL").Bold();
                r.ConstantItem(AmountWidth + 8).AlignRight().PaddingRight(8).Text(Money(invoice.Subtotal));
            });
            col.Item().PaddingTop(8).Row(r =>
            {
                r.RelativeItem().Text(VatLabel(invoice.VatPercent)).Bold();
                r.ConstantItem(AmountWidth + 8).AlignRight().PaddingRight(8).Text(Money(invoice.VatAmount));
            });
            col.Item().PaddingVertical(10).LineHorizontal(0.9f).LineColor(RuleGrey);
            col.Item().Element(c => TotalBand(c, invoice));
        });
    }

    private static void TotalBand(IContainer container, Invoice invoice)
    {
        const float tab = 104f;
        container.Layers(layers =>
        {
            layers.Layer().Svg(size =>
            {
                var w = size.Width;
                var h = size.Height;
                return $"<svg xmlns='http://www.w3.org/2000/svg' width='{F(w)}' height='{F(h)}' viewBox='0 0 {F(w)} {F(h)}'>"
                     + $"<polygon points='0,0 {F(tab + Slant)},0 {F(tab)},{F(h)} 0,{F(h)}' fill='{Green}'/>"
                     + $"<polygon points='{F(tab + Slant + 5)},0 {F(w)},0 {F(w)},{F(h)} {F(tab + 5)},{F(h)}' fill='{Yellow}'/>"
                     + "</svg>";
            });

            layers.PrimaryLayer().Height(36).Row(r =>
            {
                r.ConstantItem(tab).AlignMiddle().PaddingLeft(14).Text("TOTAL").Bold().FontSize(12).FontColor(Colors.White);
                r.RelativeItem().AlignMiddle().AlignRight().PaddingRight(12).Text(Money(invoice.Total)).ExtraBold().FontSize(14);
            });
        });
    }

    // ---- payment, terms, signature -------------------------------------------------------------------------

    // Short terms sit under the payment details, beside the signature, as one block kept together. Long terms (many lines, or
    // a lot of text) would make that block taller than a page, which cannot be kept together, so they are printed full width
    // below it and are free to flow onto the next page.
    private static bool TermsAreLong(string? terms) =>
        terms is not null && (terms.Length > 450 || terms.Count(ch => ch == '\n') >= 6);

    private static void PaymentAndSignature(ColumnDescriptor col, Invoice invoice)
    {
        var longTerms = TermsAreLong(invoice.TermsAndNotes);

        col.Item().PaddingTop(26).ShowEntire().Row(row =>
        {
            row.RelativeItem(3).PaddingRight(24).Column(left =>
            {
                left.Item().Text("PAYMENT METHOD").Bold().FontSize(11).FontColor(Green);
                PaymentRow(left, "Account Name:", invoice.PaymentAccountName);
                PaymentRow(left, "Account Number:", invoice.PaymentAccountNumber);
                PaymentRow(left, "Bank Name:", invoice.PaymentBankName);

                if (!longTerms) TermsBlock(left, invoice.TermsAndNotes, 18);
            });

            row.RelativeItem(2).PaddingTop(46).Column(sign =>
            {
                // The name wraps if it is long; the line sits under however many lines it takes.
                sign.Item().MinHeight(20).AlignBottom().AlignCenter().Text(invoice.SignatoryName ?? "").SemiBold().FontSize(9);
                sign.Item().PaddingTop(2).LineHorizontal(0.9f).LineColor(Ink);
                sign.Item().PaddingTop(4).AlignCenter().Text("Name & Signature").FontSize(9).FontColor(Muted);
            });
        });

        if (longTerms) col.Item().Column(below => TermsBlock(below, invoice.TermsAndNotes, 20));
    }

    private static void TermsBlock(ColumnDescriptor col, string? terms, float top)
    {
        col.Item().PaddingTop(top).Text("TERMS & CONDITIONS/NOTES").Bold().FontSize(11).FontColor(Green);
        if (!string.IsNullOrWhiteSpace(terms))
            col.Item().PaddingTop(5).Text(terms).FontSize(9).FontColor(Muted).LineHeight(1.4f);
    }

    private static void PaymentRow(ColumnDescriptor col, string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        col.Item().PaddingTop(4).Row(r =>
        {
            r.ConstantItem(92).Text(label).Bold();
            r.RelativeItem().Text(value);
        });
    }

    // ---- footer ------------------------------------------------------------------------------------------------------

    private static void Footer(IContainer container, InvoiceIssuer issuer)
    {
        container.PaddingTop(10).Row(row =>
        {
            row.RelativeItem().PaddingLeft(PageMargin).PaddingBottom(16).AlignBottom().Column(col =>
            {
                FooterRow(col, Icon.Location, issuer.Address);
                FooterRow(col, Icon.Phone, issuer.Phone);
                FooterRow(col, Icon.Email, issuer.Email);
                FooterRow(col, Icon.Website, issuer.Website);
            });

            row.ConstantItem(190).Height(78).AlignBottom().Svg(DecorativeLines);
        });
    }

    private static void FooterRow(ColumnDescriptor col, Icon icon, string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        col.Item().PaddingTop(5).Row(r =>
        {
            r.ConstantItem(16).Height(16).Svg(IconSvg(icon));
            r.RelativeItem().PaddingLeft(8).AlignMiddle().Text(text).FontSize(9);
        });
    }

    // Three lines coming in from the right edge, each ending in a circle.
    private static string DecorativeLines(Size size)
    {
        var w = size.Width;
        var h = size.Height;
        var ys = new[] { h - 52f, h - 34f, h - 16f };
        var lengths = new[] { 150f, 112f, 78f };
        var colours = new[] { Green, Yellow, Green };
        var sb = new System.Text.StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' width='{F(w)}' height='{F(h)}' viewBox='0 0 {F(w)} {F(h)}'>");
        for (var i = 0; i < 3; i++)
        {
            var x = w - lengths[i];
            sb.Append($"<line x1='{F(x)}' y1='{F(ys[i])}' x2='{F(w)}' y2='{F(ys[i])}' stroke='{colours[i]}' stroke-width='3'/>");
            sb.Append($"<circle cx='{F(x)}' cy='{F(ys[i])}' r='5' fill='{colours[i]}'/>");
        }
        return sb.Append("</svg>").ToString();
    }

    private enum Icon { Location, Phone, Email, Website }

    private static string IconSvg(Icon icon)
    {
        var glyph = icon switch
        {
            Icon.Location => "<path d='M8 3.3c-1.9 0-3.3 1.4-3.3 3.2 0 2.3 3.3 6.2 3.3 6.2s3.3-3.9 3.3-6.2C11.3 4.7 9.9 3.3 8 3.3z' fill='white'/><circle cx='8' cy='6.5' r='1.2' fill='" + Green + "'/>",
            Icon.Phone => "<path d='M5.3 3.6c-.6 0-1.3.6-1.3 1.3 0 3.6 3.5 7.1 7.1 7.1.7 0 1.3-.7 1.3-1.3l-.1-1.2-2-.7-.9 1c-1.2-.5-2.3-1.6-2.8-2.8l1-.9-.7-2z' fill='white'/>",
            Icon.Email => "<rect x='3.6' y='5' width='8.8' height='6' rx='.8' fill='white'/><polyline points='3.8,5.4 8,8.5 12.2,5.4' fill='none' stroke='" + Green + "' stroke-width='.9'/>",
            _ => "<circle cx='8' cy='8' r='3.9' fill='none' stroke='white' stroke-width='.9'/><ellipse cx='8' cy='8' rx='1.7' ry='3.9' fill='none' stroke='white' stroke-width='.8'/><line x1='4.1' y1='8' x2='11.9' y2='8' stroke='white' stroke-width='.8'/>",
        };
        return $"<svg xmlns='http://www.w3.org/2000/svg' width='16' height='16' viewBox='0 0 16 16'><circle cx='8' cy='8' r='8' fill='{Green}'/>{glyph}</svg>";
    }
}
