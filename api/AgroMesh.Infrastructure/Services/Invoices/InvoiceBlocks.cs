using System.Globalization;
using System.Text;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Utilities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AgroMesh.Infrastructure.Services.Invoices;

/// <summary>
/// The colours an invoice is drawn with, worked out once from the two brand colours: fills use the colours as given, text on a
/// fill is white or dark ink by contrast, and text or thin lines on white paper use the brand colour darkened until it has 3:1
/// contrast (so a pale brand colour still reads). The default NGF colours pass through unchanged.
/// </summary>
public sealed class InvoicePalette
{
    public const string Ink = BrandColor.Ink;
    public const string Muted = "#5F6B64";
    public const string GridGrey = "#C8CCD0";
    public const string RuleGrey = "#A9AEB3";

    public string Primary { get; }
    public string Accent { get; }
    public string OnPrimary { get; }
    public string OnAccent { get; }
    /// <summary>The primary colour for text and thin lines on white.</summary>
    public string PrimaryText { get; }
    public string PrimaryTint { get; }
    public string AccentTint { get; }

    public InvoicePalette(InvoiceTheme theme)
    {
        Primary = theme.Primary;
        Accent = theme.Accent;
        OnPrimary = BrandColor.TextOn(Primary);
        OnAccent = BrandColor.TextOn(Accent);
        PrimaryText = BrandColor.ForText(Primary);
        PrimaryTint = BrandColor.Tint(Primary, 0.92);
        AccentTint = BrandColor.Tint(Accent, 0.85);
    }
}

/// <summary>Everything a template needs to draw one invoice.</summary>
public sealed record InvoiceContext(Invoice Invoice, InvoiceIssuer Issuer, InvoicePalette P, Image? Logo);

public enum HeaderStyle { NgfSlant, Solid, Accent, Underline, Tinted, Hairline, Pill }
public enum GridStyle { Full, Rows, Zebra }
public enum TotalStyle { NgfBand, Solid, Accent, Underline, Boxed }
public enum IconStyle { Primary, OnPrimary, Accent }

/// <summary>How the item table looks. The column widths are sized for the largest amounts the API accepts.</summary>
public sealed record TableLook(
    HeaderStyle Header, GridStyle Grid,
    float FontSize = 9.5f, int MinRows = 5,
    float Serial = 40f, float Rate = 100f, float Qty = 50f, float Amount = 110f,
    float HPad = 8f, float VPad = 7f);

/// <summary>Building blocks shared by every invoice template, so a template is a composition rather than a copy.</summary>
public static partial class InvoiceBlocks
{
    private const float Slant = 14f;

    // ---- formatting ---------------------------------------------------------------------------------------------

    public static string Money(decimal value) => "₦" + value.ToString("N2", CultureInfo.InvariantCulture);
    public static string VatLabel(decimal percent) => $"TAX VAT ({percent.ToString("0.##", CultureInfo.InvariantCulture)}%)";
    public static string F(float v) => v.ToString("0.##", CultureInfo.InvariantCulture);
    public static string Day(DateOnly d) => d.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
    public static string Qty(decimal q) => q.ToString("0.####", CultureInfo.InvariantCulture);

    /// <summary>Phone, email, website and address on one line, for the templates that print contacts inline.</summary>
    public static string InlineContacts(InvoiceIssuer i, bool includeAddress = true) =>
        string.Join("   |   ", new[] { includeAddress ? i.Address : null, i.Phone, i.Email, i.Website }.Where(s => !string.IsNullOrWhiteSpace(s)));

    public static string Svg(float w, float h, string body) =>
        $"<svg xmlns='http://www.w3.org/2000/svg' width='{F(w)}' height='{F(h)}' viewBox='0 0 {F(w)} {F(h)}'>{body}</svg>";

    // ---- logo / name -----------------------------------------------------------------------------------------------

    public enum Side { Left, Center, Right }

    /// <summary>The farm logo fitted into a box of the given height, or the farm name in the primary colour when there is no logo.</summary>
    public static void LogoOrName(IContainer c, InvoiceContext ctx, float height, Side side, float nameSize = 15f, string? nameColor = null)
    {
        var box = c.Height(height);
        box = side switch { Side.Left => box.AlignLeft(), Side.Center => box.AlignCenter(), _ => box.AlignRight() };
        if (ctx.Logo is not null)
            box.AlignMiddle().Image(ctx.Logo).FitArea();
        else
            box.AlignMiddle().Text(ctx.Issuer.Name).FontSize(nameSize).ExtraBold().FontColor(nameColor ?? ctx.P.PrimaryText);
    }

    // ---- bill to / meta -------------------------------------------------------------------------------------------

    public static void ContactRow(ColumnDescriptor col, InvoiceContext ctx, string tag, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        col.Item().PaddingTop(3).Row(r =>
        {
            r.ConstantItem(16).Text(tag).Bold().FontColor(ctx.P.PrimaryText);
            r.RelativeItem().Text(value);
        });
    }

    /// <summary>"BILLING TO:" with an accent underline, the customer name and its P/E/A rows.</summary>
    public static void BillTo(ColumnDescriptor col, InvoiceContext ctx, string heading = "BILLING TO:", bool underline = true, float nameSize = 12f)
    {
        var inv = ctx.Invoice;
        col.Item().Text(heading).FontSize(11).Bold();
        if (underline) col.Item().PaddingTop(3).Width(56).Height(3).Background(ctx.P.Accent);
        col.Item().PaddingTop(underline ? 10 : 6).Text(inv.BillToName).FontSize(nameSize).Bold();
        ContactRow(col, ctx, "P:", inv.BillToPhone);
        ContactRow(col, ctx, "E:", inv.BillToEmail);
        ContactRow(col, ctx, "A:", inv.BillToAddress);
    }

    public static void MetaRow(IContainer container, string label, string value, float valueWidth = 92f, bool alignRight = true) =>
        container.Row(r =>
        {
            var l = r.RelativeItem();
            (alignRight ? l.AlignRight() : l).Text(label).Bold();
            r.ConstantItem(valueWidth).AlignRight().Text(value);
        });

    /// <summary>Invoice number, date and due date as three label/value rows.</summary>
    public static void Meta(ColumnDescriptor col, InvoiceContext ctx, float valueWidth = 92f, bool alignRight = true)
    {
        var inv = ctx.Invoice;
        col.Item().Element(c => MetaRow(c, "Invoice:", inv.Reference, valueWidth, alignRight));
        col.Item().PaddingTop(2).Element(c => MetaRow(c, "Date:", Day(inv.Date), valueWidth, alignRight));
        col.Item().PaddingTop(2).Element(c => MetaRow(c, "Due Date:", Day(inv.DueDate), valueWidth, alignRight));
    }

    // ---- item table ---------------------------------------------------------------------------------------------------

    public static void ItemTable(IContainer container, InvoiceContext ctx, TableLook look)
    {
        var inv = ctx.Invoice;
        container.DefaultTextStyle(t => t.FontSize(look.FontSize)).Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(look.Serial);
                c.RelativeColumn();
                c.ConstantColumn(look.Rate);
                c.ConstantColumn(look.Qty);
                c.ConstantColumn(look.Amount);
            });

            table.Header(h => h.Cell().ColumnSpan(5).Element(c => HeaderBar(c, ctx, look)));

            var rows = Math.Max(look.MinRows, inv.LineItems.Count);
            for (var i = 0; i < rows; i++)
            {
                var item = i < inv.LineItems.Count ? inv.LineItems[i] : null;
                var zebra = look.Grid == GridStyle.Zebra && i % 2 == 1;
                table.Cell().Element(c => GridCell(c, ctx, look, zebra, first: true)).AlignCenter().Text(item is null ? "" : (i + 1).ToString(CultureInfo.InvariantCulture));
                table.Cell().Element(c => GridCell(c, ctx, look, zebra)).ShowEntire().Text(item?.Description ?? "");
                table.Cell().Element(c => GridCell(c, ctx, look, zebra)).AlignRight().Text(item is null ? "" : Money(item.Rate));
                table.Cell().Element(c => GridCell(c, ctx, look, zebra)).AlignCenter().Text(item is null ? "" : Qty(item.Quantity));
                table.Cell().Element(c => GridCell(c, ctx, look, zebra)).AlignRight().Text(item is null ? "" : Money(item.Amount));
            }
        });
    }

    private static IContainer GridCell(IContainer c, InvoiceContext ctx, TableLook look, bool zebra, bool first = false)
    {
        var cell = c.MinHeight(look.VPad * 2 + 12);
        switch (look.Grid)
        {
            case GridStyle.Full:
                if (first) cell = cell.BorderLeft(0.6f).BorderColor(InvoicePalette.GridGrey);
                cell = cell.BorderBottom(0.6f).BorderRight(0.6f).BorderColor(InvoicePalette.GridGrey);
                break;
            case GridStyle.Rows:
                cell = cell.BorderBottom(0.6f).BorderColor(InvoicePalette.GridGrey);
                break;
            case GridStyle.Zebra:
                cell = zebra ? cell.Background(ctx.P.PrimaryTint) : cell;
                break;
        }
        return cell.PaddingVertical(look.VPad).PaddingHorizontal(look.HPad);
    }

    private static void HeaderBar(IContainer container, InvoiceContext ctx, TableLook look)
    {
        var p = ctx.P;
        var fixedRight = look.Rate + look.Qty + look.Amount;
        string leftText, rightText;
        switch (look.Header)
        {
            case HeaderStyle.NgfSlant: leftText = p.OnAccent; rightText = p.OnPrimary; break;
            case HeaderStyle.Solid: leftText = rightText = p.OnPrimary; break;
            case HeaderStyle.Accent: leftText = rightText = p.OnAccent; break;
            case HeaderStyle.Pill: leftText = rightText = p.OnPrimary; break;
            case HeaderStyle.Hairline: leftText = rightText = InvoicePalette.Muted; break;
            default: leftText = rightText = p.PrimaryText; break;   // Underline, Tinted
        }

        IContainer Row(IContainer c) => look.Header switch
        {
            HeaderStyle.Solid => c.Background(p.Primary),
            HeaderStyle.Accent => c.Background(p.Accent),
            HeaderStyle.Tinted => c.Background(p.PrimaryTint).BorderBottom(1.5f).BorderColor(p.PrimaryText),
            HeaderStyle.Underline => c.BorderBottom(1.5f).BorderColor(p.PrimaryText),
            HeaderStyle.Hairline => c.BorderBottom(0.8f).BorderColor(InvoicePalette.RuleGrey),
            HeaderStyle.Pill => c.CornerRadius(8).Background(p.Primary),
            _ => c,
        };

        void Texts(IContainer c) => c.Height(30).Row(r =>
        {
            r.ConstantItem(look.Serial).AlignMiddle().AlignCenter().Text("S/N").Bold().FontSize(9).FontColor(leftText);
            r.RelativeItem().AlignMiddle().PaddingLeft(look.HPad).Text("ITEM DESCRIPTION").Bold().FontSize(9).FontColor(leftText);
            r.ConstantItem(look.Rate).AlignMiddle().PaddingRight(look.HPad).AlignRight().Text("RATE").Bold().FontSize(9).FontColor(rightText);
            r.ConstantItem(look.Qty).AlignMiddle().AlignCenter().Text("QTY").Bold().FontSize(9).FontColor(rightText);
            r.ConstantItem(look.Amount).AlignMiddle().PaddingRight(look.HPad).AlignRight().Text("AMOUNT").Bold().FontSize(9).FontColor(rightText);
        });

        if (look.Header != HeaderStyle.NgfSlant)
        {
            Row(container).Element(Texts);
            return;
        }

        container.Layers(layers =>
        {
            layers.Layer().Svg(size =>
            {
                var w = size.Width;
                var h = size.Height;
                var split = w - fixedRight;            // where the accent tab ends and the primary bar starts
                return Svg(w, h,
                    $"<polygon points='0,0 {F(split + Slant)},0 {F(split)},{F(h)} 0,{F(h)}' fill='{p.Accent}'/>"
                  + $"<polygon points='{F(split + Slant + 5)},0 {F(w)},0 {F(w)},{F(h)} {F(split + 5)},{F(h)}' fill='{p.Primary}'/>");
            });
            layers.PrimaryLayer().Element(Texts);
        });
    }

    // ---- totals --------------------------------------------------------------------------------------------------------

    public static void Totals(IContainer container, InvoiceContext ctx, TotalStyle style, float width, float amountWidth = 110f)
    {
        var inv = ctx.Invoice;
        container.AlignRight().Width(width).Column(col =>
        {
            col.Item().Row(r =>
            {
                r.RelativeItem().Text("SUBTOTAL").Bold();
                r.ConstantItem(amountWidth + 8).AlignRight().PaddingRight(8).Text(Money(inv.Subtotal));
            });
            col.Item().PaddingTop(8).Row(r =>
            {
                r.RelativeItem().Text(VatLabel(inv.VatPercent)).Bold();
                r.ConstantItem(amountWidth + 8).AlignRight().PaddingRight(8).Text(Money(inv.VatAmount));
            });
            col.Item().PaddingVertical(10).LineHorizontal(0.9f).LineColor(InvoicePalette.RuleGrey);
            col.Item().Element(c => TotalBand(c, ctx, style));
        });
    }

    private static void TotalBand(IContainer container, InvoiceContext ctx, TotalStyle style)
    {
        var p = ctx.P;
        var total = Money(ctx.Invoice.Total);
        switch (style)
        {
            case TotalStyle.NgfBand:
                const float tab = 104f;
                container.Layers(layers =>
                {
                    layers.Layer().Svg(size =>
                    {
                        var w = size.Width;
                        var h = size.Height;
                        return Svg(w, h,
                            $"<polygon points='0,0 {F(tab + Slant)},0 {F(tab)},{F(h)} 0,{F(h)}' fill='{p.Primary}'/>"
                          + $"<polygon points='{F(tab + Slant + 5)},0 {F(w)},0 {F(w)},{F(h)} {F(tab + 5)},{F(h)}' fill='{p.Accent}'/>");
                    });
                    layers.PrimaryLayer().Height(36).Row(r =>
                    {
                        r.ConstantItem(tab).AlignMiddle().PaddingLeft(14).Text("TOTAL").Bold().FontSize(12).FontColor(p.OnPrimary);
                        r.RelativeItem().AlignMiddle().AlignRight().PaddingRight(12).Text(total).ExtraBold().FontSize(14).FontColor(p.OnAccent);
                    });
                });
                break;
            case TotalStyle.Solid:
            case TotalStyle.Accent:
                var fill = style == TotalStyle.Solid ? p.Primary : p.Accent;
                var text = style == TotalStyle.Solid ? p.OnPrimary : p.OnAccent;
                container.Background(fill).Height(36).Row(r =>
                {
                    r.RelativeItem().AlignMiddle().PaddingLeft(14).Text("TOTAL").Bold().FontSize(12).FontColor(text);
                    r.AutoItem().AlignMiddle().PaddingRight(12).Text(total).ExtraBold().FontSize(14).FontColor(text);
                });
                break;
            case TotalStyle.Boxed:
                container.Background(p.PrimaryTint).Border(1).BorderColor(p.PrimaryText).Height(36).Row(r =>
                {
                    r.RelativeItem().AlignMiddle().PaddingLeft(12).Text("TOTAL").Bold().FontSize(12).FontColor(p.PrimaryText);
                    r.AutoItem().AlignMiddle().PaddingRight(12).Text(total).ExtraBold().FontSize(14).FontColor(p.PrimaryText);
                });
                break;
            default:
                container.BorderTop(2).BorderColor(p.PrimaryText).PaddingTop(6).Row(r =>
                {
                    r.RelativeItem().AlignMiddle().Text("TOTAL").Bold().FontSize(12).FontColor(p.PrimaryText);
                    r.AutoItem().AlignMiddle().PaddingRight(8).Text(total).ExtraBold().FontSize(15).FontColor(p.PrimaryText);
                });
                break;
        }
    }

    // ---- payment, terms, signature ---------------------------------------------------------------------------------

    /// <summary>Terms over 450 characters or 6 lines would make a kept-together block taller than a page, so they flow on their own.</summary>
    public static bool TermsAreLong(string? terms) =>
        terms is not null && (terms.Length > 450 || terms.Count(ch => ch == '\n') >= 6);

    public static void PaymentBlock(ColumnDescriptor col, InvoiceContext ctx, string heading = "PAYMENT METHOD", float labelWidth = 92f, string? headingColor = null, bool stacked = false)
    {
        var inv = ctx.Invoice;
        col.Item().Text(heading).Bold().FontSize(11).FontColor(headingColor ?? ctx.P.PrimaryText);
        PaymentRow(col, "Account Name:", inv.PaymentAccountName, labelWidth, stacked);
        PaymentRow(col, "Account Number:", inv.PaymentAccountNumber, labelWidth, stacked);
        PaymentRow(col, "Bank Name:", inv.PaymentBankName, labelWidth, stacked);
    }

    private static void PaymentRow(ColumnDescriptor col, string label, string? value, float labelWidth, bool stacked)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        if (stacked)
        {
            // Narrow columns (a sidebar): the label above its value, so neither wraps mid-word.
            col.Item().PaddingTop(6).Text(label).Bold().FontSize(8);
            col.Item().Text(value);
            return;
        }
        col.Item().PaddingTop(4).Row(r =>
        {
            r.ConstantItem(labelWidth).Text(label).Bold();
            r.RelativeItem().Text(value);
        });
    }

    public static void TermsBlock(ColumnDescriptor col, InvoiceContext ctx, float top, string heading = "TERMS & CONDITIONS/NOTES")
    {
        col.Item().PaddingTop(top).Text(heading).Bold().FontSize(11).FontColor(ctx.P.PrimaryText);
        if (!string.IsNullOrWhiteSpace(ctx.Invoice.TermsAndNotes))
            col.Item().PaddingTop(5).Text(ctx.Invoice.TermsAndNotes).FontSize(9).FontColor(InvoicePalette.Muted).LineHeight(1.4f);
    }

    /// <summary>The signatory name (wrapping if long) above a line labelled "Name &amp; Signature".</summary>
    public static void Signature(IContainer container, InvoiceContext ctx, float topPadding = 46f)
    {
        container.PaddingTop(topPadding).Column(sign =>
        {
            sign.Item().MinHeight(20).AlignBottom().AlignCenter().Text(ctx.Invoice.SignatoryName ?? "").SemiBold().FontSize(9);
            sign.Item().PaddingTop(2).LineHorizontal(0.9f).LineColor(InvoicePalette.Ink);
            sign.Item().PaddingTop(4).AlignCenter().Text("Name & Signature").FontSize(9).FontColor(InvoicePalette.Muted);
        });
    }

    /// <summary>
    /// The NGF arrangement: payment details (with short terms under them) beside the signature, kept together. Long terms print
    /// full width below it and flow across pages.
    /// </summary>
    public static void PaymentTermsSignature(ColumnDescriptor col, InvoiceContext ctx, float top = 26f)
    {
        var longTerms = TermsAreLong(ctx.Invoice.TermsAndNotes);

        col.Item().PaddingTop(top).ShowEntire().Row(row =>
        {
            row.RelativeItem(3).PaddingRight(24).Column(left =>
            {
                PaymentBlock(left, ctx);
                if (!longTerms) TermsBlock(left, ctx, 18);
            });
            row.RelativeItem(2).Element(c => Signature(c, ctx));
        });

        if (longTerms) col.Item().Column(below => TermsBlock(below, ctx, 20));
    }

    /// <summary>Terms beside the signature, kept together; long terms print full width below and flow. For templates that show the payment details elsewhere.</summary>
    public static void TermsAndSignature(ColumnDescriptor col, InvoiceContext ctx, float top = 26f)
    {
        var longTerms = TermsAreLong(ctx.Invoice.TermsAndNotes);

        col.Item().PaddingTop(top).ShowEntire().Row(row =>
        {
            row.RelativeItem(3).PaddingRight(24).Column(left =>
            {
                if (!longTerms) TermsBlock(left, ctx, 0);
            });
            row.RelativeItem(2).Element(c => Signature(c, ctx));
        });

        if (longTerms) col.Item().Column(below => TermsBlock(below, ctx, 20));
    }

    // ---- contacts and decoration ----------------------------------------------------------------------------------

    public enum Icon { Location, Phone, Email, Website }

    public static string IconSvg(Icon icon, string circle, string glyph)
    {
        var g = icon switch
        {
            Icon.Location => $"<path d='M8 3.3c-1.9 0-3.3 1.4-3.3 3.2 0 2.3 3.3 6.2 3.3 6.2s3.3-3.9 3.3-6.2C11.3 4.7 9.9 3.3 8 3.3z' fill='{glyph}'/><circle cx='8' cy='6.5' r='1.2' fill='{circle}'/>",
            Icon.Phone => $"<path d='M5.3 3.6c-.6 0-1.3.6-1.3 1.3 0 3.6 3.5 7.1 7.1 7.1.7 0 1.3-.7 1.3-1.3l-.1-1.2-2-.7-.9 1c-1.2-.5-2.3-1.6-2.8-2.8l1-.9-.7-2z' fill='{glyph}'/>",
            Icon.Email => $"<rect x='3.6' y='5' width='8.8' height='6' rx='.8' fill='{glyph}'/><polyline points='3.8,5.4 8,8.5 12.2,5.4' fill='none' stroke='{circle}' stroke-width='.9'/>",
            _ => $"<circle cx='8' cy='8' r='3.9' fill='none' stroke='{glyph}' stroke-width='.9'/><ellipse cx='8' cy='8' rx='1.7' ry='3.9' fill='none' stroke='{glyph}' stroke-width='.8'/><line x1='4.1' y1='8' x2='11.9' y2='8' stroke='{glyph}' stroke-width='.8'/>",
        };
        return $"<svg xmlns='http://www.w3.org/2000/svg' width='16' height='16' viewBox='0 0 16 16'><circle cx='8' cy='8' r='8' fill='{circle}'/>{g}</svg>";
    }

    /// <summary>Contact rows (address, phone, email, website), each with a round icon.</summary>
    public static void IconRows(ColumnDescriptor col, InvoiceContext ctx, IconStyle style, string textColor, float fontSize = 9f, float gap = 5f)
    {
        var (circle, glyph) = style switch
        {
            IconStyle.OnPrimary => (ctx.P.OnPrimary, ctx.P.Primary),
            IconStyle.Accent => (ctx.P.Accent, ctx.P.OnAccent),
            _ => (ctx.P.Primary, ctx.P.OnPrimary),
        };
        void Row(Icon icon, string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            col.Item().PaddingTop(gap).Row(r =>
            {
                r.ConstantItem(16).Height(16).Svg(IconSvg(icon, circle, glyph));
                r.RelativeItem().PaddingLeft(8).AlignMiddle().Text(text).FontSize(fontSize).FontColor(textColor);
            });
        }
        Row(Icon.Location, ctx.Issuer.Address);
        Row(Icon.Phone, ctx.Issuer.Phone);
        Row(Icon.Email, ctx.Issuer.Email);
        Row(Icon.Website, ctx.Issuer.Website);
    }

    /// <summary>Plain contact lines (no icons), one per detail.</summary>
    public static void ContactLines(ColumnDescriptor col, InvoiceContext ctx, string color, float fontSize = 9f, Side side = Side.Left)
    {
        foreach (var line in new[] { ctx.Issuer.Address, ctx.Issuer.Phone, ctx.Issuer.Email, ctx.Issuer.Website })
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var item = col.Item().PaddingTop(2);
            var aligned = side switch { Side.Left => item.AlignLeft(), Side.Center => item.AlignCenter(), _ => item.AlignRight() };
            aligned.Text(line).FontSize(fontSize).FontColor(color);
        }
    }

    /// <summary>Three horizontal lines with round ends, coming in from the right edge of the box they are drawn in.</summary>
    public static string DecorativeLines(Size size, string c1, string c2)
    {
        var w = size.Width;
        var h = size.Height;
        var ys = new[] { h - 52f, h - 34f, h - 16f };
        var lengths = new[] { 150f, 112f, 78f };
        var colours = new[] { c1, c2, c1 };
        var sb = new StringBuilder();
        for (var i = 0; i < 3; i++)
        {
            var x = w - lengths[i];
            sb.Append($"<line x1='{F(x)}' y1='{F(ys[i])}' x2='{F(w)}' y2='{F(ys[i])}' stroke='{colours[i]}' stroke-width='3'/>");
            sb.Append($"<circle cx='{F(x)}' cy='{F(ys[i])}' r='5' fill='{colours[i]}'/>");
        }
        return Svg(w, h, sb.ToString());
    }

    /// <summary>A full-width dashed rule, for the receipt-style template.</summary>
    public static string DashedRule(Size size, string colour) =>
        Svg(size.Width, size.Height, $"<line x1='0' y1='{F(size.Height / 2)}' x2='{F(size.Width)}' y2='{F(size.Height / 2)}' stroke='{colour}' stroke-width='1' stroke-dasharray='4 3'/>");

    /// <summary>Two flat coloured blocks side by side with a slanted join: the left one {leftShare} of the width.</summary>
    public static string TwoToneBand(Size size, string left, string right, float leftShare)
    {
        var w = size.Width;
        var h = size.Height;
        var split = w * leftShare;
        return Svg(w, h,
            $"<rect x='0' y='0' width='{F(w)}' height='{F(h)}' fill='{right}'/>"
          + $"<polygon points='0,0 {F(split + Slant)},0 {F(split - Slant)},{F(h)} 0,{F(h)}' fill='{left}'/>");
    }

    public static string SolidRect(Size size, string colour) =>
        Svg(size.Width, size.Height, $"<rect x='0' y='0' width='{F(size.Width)}' height='{F(size.Height)}' fill='{colour}'/>");
}
