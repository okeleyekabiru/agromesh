using System.Globalization;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Utilities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AgroMesh.Infrastructure.Services.Invoices;

/// <summary>Building blocks for the contemporary templates: small-caps labels, stacked facts, rounded cards and big figures.</summary>
public static partial class InvoiceBlocks
{
    /// <summary>A small, letter-spaced capitals label ("BILLED TO", "DUE DATE").</summary>
    public static void Label(IContainer c, string text, string color = InvoicePalette.Muted, float size = 7.5f) =>
        c.Text(text.ToUpperInvariant()).FontSize(size).SemiBold().LetterSpacing(0.08f).FontColor(color);

    /// <summary>"BILLED TO" label, the customer name in bold and its details as plain lines (no P/E/A tags).</summary>
    public static void BillToClean(ColumnDescriptor col, InvoiceContext ctx, string heading = "Billed to", string? labelColor = null, string? textColor = null, float nameSize = 13f)
    {
        var inv = ctx.Invoice;
        var color = textColor ?? InvoicePalette.Ink;
        col.Item().Element(c => Label(c, heading, labelColor ?? InvoicePalette.Muted));
        col.Item().PaddingTop(4).Text(inv.BillToName).FontSize(nameSize).Bold().FontColor(color);
        foreach (var line in new[] { inv.BillToAddress, inv.BillToPhone, inv.BillToEmail })
            if (!string.IsNullOrWhiteSpace(line))
                col.Item().PaddingTop(2).Text(line).FontColor(textColor ?? InvoicePalette.Muted);
    }

    /// <summary>One label above its value.</summary>
    public static void Fact(ColumnDescriptor col, string label, string value, string? labelColor = null, string? valueColor = null, float valueSize = 10f, float top = 0f)
    {
        col.Item().PaddingTop(top).Element(c => Label(c, label, labelColor ?? InvoicePalette.Muted));
        col.Item().PaddingTop(2).Text(value).FontSize(valueSize).SemiBold().FontColor(valueColor ?? InvoicePalette.Ink);
    }

    /// <summary>Invoice number, issue date and due date as three side-by-side facts.</summary>
    public static void FactRow(IContainer c, InvoiceContext ctx, string? labelColor = null, string? valueColor = null)
    {
        var inv = ctx.Invoice;
        c.Row(r =>
        {
            r.RelativeItem().Column(x => Fact(x, "Invoice no.", inv.Reference, labelColor, valueColor));
            r.RelativeItem().Column(x => Fact(x, "Issued", Day(inv.Date), labelColor, valueColor));
            r.RelativeItem().Column(x => Fact(x, "Due", Day(inv.DueDate), labelColor, valueColor));
        });
    }

    /// <summary>Subtotal and VAT as two label/value rows (no total), for layouts that show the total somewhere bigger.</summary>
    public static void SubtotalAndVat(ColumnDescriptor col, InvoiceContext ctx, string? color = null, float size = 9.5f)
    {
        var inv = ctx.Invoice;
        void Row(string label, string value) => col.Item().PaddingTop(4).Row(r =>
        {
            r.RelativeItem().Text(label).FontSize(size).FontColor(color ?? InvoicePalette.Muted);
            r.AutoItem().Text(value).FontSize(size).SemiBold().FontColor(color ?? InvoicePalette.Ink);
        });
        Row("Subtotal", Money(inv.Subtotal));
        Row(VatLabel(inv.VatPercent).Replace("TAX VAT", "VAT"), Money(inv.VatAmount));
    }

    /// <summary>Payment lines (account name, number, bank) without a heading, as stacked facts.</summary>
    public static void PaymentFacts(ColumnDescriptor col, InvoiceContext ctx, string? labelColor = null, string? valueColor = null)
    {
        var inv = ctx.Invoice;
        void One(string label, string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            Fact(col, label, value, labelColor, valueColor, 9.5f, 6);
        }
        One("Account name", inv.PaymentAccountName);
        One("Account number", inv.PaymentAccountNumber);
        One("Bank", inv.PaymentBankName);
    }

    /// <summary>The terms text under a small label. Long terms flow across pages like any text.</summary>
    public static void TermsText(ColumnDescriptor col, InvoiceContext ctx, string heading = "Terms & notes")
    {
        if (string.IsNullOrWhiteSpace(ctx.Invoice.TermsAndNotes)) return;
        col.Item().Element(c => Label(c, heading));
        col.Item().PaddingTop(4).Text(ctx.Invoice.TermsAndNotes).FontSize(9).FontColor(InvoicePalette.Muted).LineHeight(1.45f);
    }

    /// <summary>Payment facts beside the signature (kept together), with the terms below, full width, so long terms can flow.</summary>
    public static void PaymentAndSignatureClean(ColumnDescriptor col, InvoiceContext ctx, float top = 28f)
    {
        col.Item().PaddingTop(top).ShowEntire().Row(r =>
        {
            r.RelativeItem(3).PaddingRight(24).Column(c =>
            {
                c.Item().Element(x => Label(x, "Payment details"));
                PaymentFacts(c, ctx);
            });
            r.RelativeItem(2).Element(c => Signature(c, ctx, 30));
        });
        if (!string.IsNullOrWhiteSpace(ctx.Invoice.TermsAndNotes))
            col.Item().PaddingTop(18).Column(c => TermsText(c, ctx));
    }

    /// <summary>The page opens with the farm contact details as plain lines or inline; used in clean footers.</summary>
    public static void FooterLine(IContainer c, InvoiceContext ctx, string color = InvoicePalette.Muted) =>
        c.Text(InlineContacts(ctx.Issuer)).FontSize(8).FontColor(color);

    /// <summary>A rounded filled card around some content.</summary>
    public static IContainer Card(IContainer c, string fill, float radius = 12f, float padding = 16f) =>
        c.CornerRadius(radius).Background(fill).Padding(padding);

    /// <summary>The logo on a white rounded plate (so any logo reads on a coloured block), or the farm name in white when there is none.</summary>
    public static void LogoPlate(IContainer c, InvoiceContext ctx, float height, string nameColor)
    {
        if (ctx.Logo is not null)
            c.AlignLeft().CornerRadius(10).Background(BrandColor.White).Padding(8).Height(height).Image(ctx.Logo).FitArea();
        else
            c.AlignLeft().AlignMiddle().Text(ctx.Issuer.Name).FontSize(17).ExtraBold().FontColor(nameColor);
    }

    /// <summary>The largest font size (up to max) at which the text still fits the width: for big figures in narrow places.</summary>
    public static float FitSize(string text, float width, float max) => Math.Clamp(width / (Math.Max(text.Length, 1) * 0.68f), 11f, max);

    /// <summary>A sidebar column and a thin accent edge drawn behind the page, left side.</summary>
    public static string SidebarBackground(Size size, float width, string fill, string edge, float edgeWidth = 5f) =>
        Svg(size.Width, size.Height,
            $"<rect x='0' y='0' width='{F(width)}' height='{F(size.Height)}' fill='{fill}'/>"
          + $"<rect x='{F(width)}' y='0' width='{F(edgeWidth)}' height='{F(size.Height)}' fill='{edge}'/>");

    /// <summary>An angled header: a colour field whose lower edge slopes from low on the left to high on the right, with a thinner accent stripe parallel below it.</summary>
    public static string AngledHeader(Size size, string fill, string stripe, float leftDrop, float rightDrop)
    {
        var w = size.Width;
        return Svg(w, size.Height,
            $"<polygon points='0,0 {F(w)},0 {F(w)},{F(rightDrop)} 0,{F(leftDrop)}' fill='{fill}'/>"
          + $"<polygon points='0,{F(leftDrop + 8)} {F(w)},{F(rightDrop + 8)} {F(w)},{F(rightDrop + 18)} 0,{F(leftDrop + 18)}' fill='{stripe}'/>");
    }

    public static string Padded(int number) => number.ToString("D4", CultureInfo.InvariantCulture);
}
