using Ilevora.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Ilevora.Infrastructure.Services.Invoices;

/// <summary>"Minimal": one calm column, an oversized invoice number and nothing but hairlines.</summary>
public class MinimalInvoiceTemplate : InvoiceTemplateBase
{
    private static readonly TableLook Look = new(HeaderStyle.Hairline, GridStyle.Rows, 9f, 3, 30f, 100f, 50f, 110f, 6f, 9f);

    public override string Key => InvoiceTemplateKeys.Minimal;
    public override string Name => "Minimal";
    public override string Description => "Generous whitespace, an oversized invoice number and hairline rules. Nothing decorative.";
    public override string DefaultPrimaryColor => "#111827";
    public override string DefaultAccentColor => "#EF4444";

    protected override void Layout(PageDescriptor page, InvoiceContext ctx)
    {
        page.Margin(0);
        var inv = ctx.Invoice;

        page.Content().PaddingHorizontal(56).PaddingTop(40).Column(col =>
        {
            col.Item().Row(r =>
            {
                r.RelativeItem().Element(c => InvoiceBlocks.LogoOrName(c, ctx, 36, InvoiceBlocks.Side.Left, 13));
                r.ConstantItem(220).Column(c => InvoiceBlocks.ContactLines(c, ctx, InvoicePalette.Muted, 8, InvoiceBlocks.Side.Right));
            });

            col.Item().PaddingTop(26).Row(r =>
            {
                r.AutoItem().Text("Invoice").FontSize(11).SemiBold().FontColor(InvoicePalette.Muted);
                r.AutoItem().PaddingLeft(6).PaddingTop(4).Width(8).Height(8).Background(ctx.P.Accent);
            });
            col.Item().Text(InvoiceBlocks.Padded(inv.Number)).FontSize(54).Bold().FontColor(ctx.P.PrimaryText);
            col.Item().PaddingTop(6).Height(0.8f).Background(InvoicePalette.RuleGrey);

            col.Item().PaddingTop(18).Row(r =>
            {
                r.RelativeItem(2).Column(c => InvoiceBlocks.BillToClean(c, ctx));
                r.RelativeItem(3).Element(c => InvoiceBlocks.FactRow(c, ctx));
            });

            col.Item().PaddingTop(24).Element(c => InvoiceBlocks.ItemTable(c, ctx, Look));
            col.Item().PaddingTop(16).Element(c => InvoiceBlocks.Totals(c, ctx, TotalStyle.Underline, 290));
            InvoiceBlocks.PaymentAndSignatureClean(col, ctx, 32);
        });

        page.Footer().PaddingHorizontal(56).PaddingBottom(26).Element(c => InvoiceBlocks.FooterLine(c, ctx));
    }
}

/// <summary>"Cards": soft rounded cards for the customer, the dates and the payment details, a pill table header and a solid total card.</summary>
public class CardsInvoiceTemplate : InvoiceTemplateBase
{
    private static readonly TableLook Look = new(HeaderStyle.Pill, GridStyle.Rows, 9.5f, 3, 36f, 100f, 50f, 110f, 8f, 8f);

    public override string Key => InvoiceTemplateKeys.Cards;
    public override string Name => "Cards";
    public override string Description => "Rounded, softly tinted cards for customer, dates and payment, with a pill table header and a solid total card.";
    public override string DefaultPrimaryColor => "#0D9488";
    public override string DefaultAccentColor => "#FDBA74";

    protected override void Layout(PageDescriptor page, InvoiceContext ctx)
    {
        page.Margin(0);
        var inv = ctx.Invoice;
        var total = InvoiceBlocks.Money(inv.Total);

        page.Content().PaddingHorizontal(40).PaddingTop(36).Column(col =>
        {
            col.Item().Row(r =>
            {
                r.RelativeItem().Element(c => InvoiceBlocks.LogoOrName(c, ctx, 52, InvoiceBlocks.Side.Left, 17));
                r.ConstantItem(220).Column(c =>
                {
                    c.Item().AlignRight().Text("Invoice").FontSize(30).ExtraBold().FontColor(ctx.P.PrimaryText);
                    c.Item().AlignRight().Text(inv.Reference).FontSize(11).SemiBold().FontColor(InvoicePalette.Muted);
                });
            });

            col.Item().PaddingTop(22).Row(r =>
            {
                InvoiceBlocks.Card(r.RelativeItem(3).PaddingRight(12), ctx.P.PrimaryTint).Column(c => InvoiceBlocks.BillToClean(c, ctx));
                InvoiceBlocks.Card(r.RelativeItem(2), ctx.P.AccentTint).Column(c =>
                {
                    InvoiceBlocks.Fact(c, "Issued", InvoiceBlocks.Day(inv.Date));
                    InvoiceBlocks.Fact(c, "Due", InvoiceBlocks.Day(inv.DueDate), top: 10);
                });
            });

            col.Item().PaddingTop(22).Element(c => InvoiceBlocks.ItemTable(c, ctx, Look));

            col.Item().PaddingTop(18).ShowEntire().Row(r =>
            {
                InvoiceBlocks.Card(r.RelativeItem(3).PaddingRight(12), ctx.P.PrimaryTint).Column(c =>
                {
                    c.Item().Element(x => InvoiceBlocks.Label(x, "Payment details"));
                    InvoiceBlocks.PaymentFacts(c, ctx);
                });
                InvoiceBlocks.Card(r.ConstantItem(236), ctx.P.Primary).Column(c =>
                {
                    InvoiceBlocks.SubtotalAndVat(c, ctx, ctx.P.OnPrimary);
                    c.Item().PaddingTop(8).Height(0.8f).Background(ctx.P.OnPrimary);
                    c.Item().PaddingTop(8).Element(x => InvoiceBlocks.Label(x, "Total", ctx.P.OnPrimary));
                    c.Item().PaddingTop(2).Text(total).FontSize(InvoiceBlocks.FitSize(total, 200, 20)).ExtraBold().FontColor(ctx.P.OnPrimary);
                });
            });

            col.Item().PaddingTop(22).Row(r =>
            {
                r.RelativeItem(3).PaddingRight(24).Column(c => InvoiceBlocks.TermsText(c, ctx));
                r.RelativeItem(2).Element(c => InvoiceBlocks.Signature(c, ctx, 24));
            });
        });

        page.Footer().PaddingHorizontal(40).PaddingBottom(20).Element(c =>
            InvoiceBlocks.Card(c, ctx.P.PrimaryTint, 10, 8).AlignCenter().Text(InvoiceBlocks.InlineContacts(ctx.Issuer)).FontSize(8).FontColor(InvoicePalette.Muted));
    }
}

/// <summary>"Ledger": a narrow, centred, receipt-style column with dashed separators and a pill total. Compact and quick to scan.</summary>
public class LedgerInvoiceTemplate : InvoiceTemplateBase
{
    private const float Width = 360f;
    private static readonly TableLook Look = new(HeaderStyle.Hairline, GridStyle.Rows, 8f, 0, 22f, 74f, 34f, 80f, 3f, 3f);

    public override string Key => InvoiceTemplateKeys.Ledger;
    public override string Name => "Ledger";
    public override string Description => "A compact, centred receipt-style invoice in small type with dashed separators. Best for short invoices.";
    public override string DefaultPrimaryColor => "#1F2937";
    public override string DefaultAccentColor => "#9CA3AF";
    protected override float BaseFontSize => 8.5f;

    private static void Dashes(ColumnDescriptor col, InvoiceContext ctx) =>
        col.Item().PaddingVertical(5).Height(6).Svg(size => InvoiceBlocks.DashedRule(size, ctx.P.Accent));

    protected override void Layout(PageDescriptor page, InvoiceContext ctx)
    {
        page.Margin(0);
        var inv = ctx.Invoice;

        page.Content().PaddingHorizontal((595f - Width) / 2).PaddingTop(28).Column(col =>
        {
            col.Item().Element(c => InvoiceBlocks.LogoOrName(c, ctx, 40, InvoiceBlocks.Side.Center, 14));
            if (ctx.Logo is not null)
                col.Item().PaddingTop(6).AlignCenter().Text(ctx.Issuer.Name).Bold().FontSize(11).FontColor(ctx.P.PrimaryText);
            col.Item().PaddingTop(4).Column(c => InvoiceBlocks.ContactLines(c, ctx, InvoicePalette.Muted, 8, InvoiceBlocks.Side.Center));
            Dashes(col, ctx);

            col.Item().AlignCenter().Element(c => InvoiceBlocks.Label(c, "Invoice " + inv.Reference, ctx.P.PrimaryText, 9));
            col.Item().PaddingTop(6).Column(m => InvoiceBlocks.Meta(m, ctx, 100, alignRight: false));
            Dashes(col, ctx);

            col.Item().Column(c => InvoiceBlocks.BillToClean(c, ctx, nameSize: 10));
            Dashes(col, ctx);

            col.Item().Element(c => InvoiceBlocks.ItemTable(c, ctx, Look));
            Dashes(col, ctx);

            col.Item().Element(c => InvoiceBlocks.Totals(c, ctx, TotalStyle.Underline, Width, Look.Amount));
            Dashes(col, ctx);

            col.Item().ShowEntire().Column(c =>
            {
                c.Item().Element(x => InvoiceBlocks.Label(x, "Payment details"));
                InvoiceBlocks.PaymentFacts(c, ctx);
            });
            col.Item().PaddingTop(10).Column(c => InvoiceBlocks.TermsText(c, ctx));
            col.Item().PaddingHorizontal(60).ShowEntire().Element(c => InvoiceBlocks.Signature(c, ctx, 14));
        });

        page.Footer().PaddingBottom(24).Column(f =>
        {
            f.Item().AlignCenter().Text("Thank you for your business").SemiBold().FontSize(9).FontColor(ctx.P.PrimaryText);
            f.Item().PaddingTop(4).AlignCenter().Text(InvoiceBlocks.InlineContacts(ctx.Issuer, includeAddress: false)).FontSize(7.5f).FontColor(InvoicePalette.Muted);
        });
    }
}
