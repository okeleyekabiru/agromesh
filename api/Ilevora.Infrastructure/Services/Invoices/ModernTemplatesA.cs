using Ilevora.Domain.Entities;
using Ilevora.Domain.Utilities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Ilevora.Infrastructure.Services.Invoices;

/// <summary>"Band": a full-width colour band carrying the logo and the invoice title, an accent edge, then a clean body with soft zebra rows.</summary>
public class BandInvoiceTemplate : InvoiceTemplateBase
{
    private static readonly TableLook Look = new(HeaderStyle.Hairline, GridStyle.Zebra, 9.5f, 4, 36f, 100f, 50f, 110f, 8f, 8f);

    public override string Key => InvoiceTemplateKeys.Band;
    public override string Name => "Band";
    public override string Description => "A bold full-width colour header with the logo and title, soft zebra rows and a clean footer.";
    public override string DefaultPrimaryColor => "#4F46E5";
    public override string DefaultAccentColor => "#F59E0B";

    protected override void Layout(PageDescriptor page, InvoiceContext ctx)
    {
        page.Margin(0);

        page.Content().Column(col =>
        {
            col.Item().Background(ctx.P.Primary).PaddingHorizontal(44).PaddingVertical(30).Row(r =>
            {
                r.RelativeItem().AlignMiddle().Element(c => InvoiceBlocks.LogoPlate(c, ctx, 48, ctx.P.OnPrimary));
                r.ConstantItem(240).Column(c =>
                {
                    c.Item().AlignRight().Text("Invoice").FontSize(34).ExtraBold().FontColor(ctx.P.OnPrimary);
                    c.Item().AlignRight().Text(ctx.Invoice.Reference).FontSize(11).SemiBold().FontColor(ctx.P.OnPrimary);
                });
            });
            col.Item().Height(5).Background(ctx.P.Accent);

            col.Item().PaddingHorizontal(44).PaddingTop(28).Column(body =>
            {
                body.Item().Row(r =>
                {
                    r.RelativeItem(3).Column(c => InvoiceBlocks.BillToClean(c, ctx));
                    r.RelativeItem(2).Column(c =>
                    {
                        InvoiceBlocks.Fact(c, "Issued", InvoiceBlocks.Day(ctx.Invoice.Date));
                        InvoiceBlocks.Fact(c, "Due", InvoiceBlocks.Day(ctx.Invoice.DueDate), top: 10);
                    });
                });
                body.Item().PaddingTop(26).Element(c => InvoiceBlocks.ItemTable(c, ctx, Look));
                body.Item().PaddingTop(14).Element(c => InvoiceBlocks.Totals(c, ctx, TotalStyle.Accent, 320));
                InvoiceBlocks.PaymentAndSignatureClean(body, ctx);
            });
        });

        page.Footer().PaddingHorizontal(44).PaddingBottom(22).Column(f =>
        {
            f.Item().Height(0.8f).Background(InvoicePalette.RuleGrey);
            f.Item().PaddingTop(8).Element(c => InvoiceBlocks.FooterLine(c, ctx));
        });
    }
}

/// <summary>"Sidebar": a coloured column down the left of every page; on page one it carries the logo, contacts, the total due and the payment details.</summary>
public class SidebarInvoiceTemplate : InvoiceTemplateBase
{
    private const float Side = 170f;
    private const float Edge = 5f;
    private const float Gap = 26f;
    private static readonly TableLook Look = new(HeaderStyle.Hairline, GridStyle.Rows, 8.5f, 3, 24f, 80f, 36f, 92f, 5f, 6f);

    public override string Key => InvoiceTemplateKeys.Sidebar;
    public override string Name => "Sidebar";
    public override string Description => "A coloured sidebar holding the logo, contacts, total due and payment details beside a clean item list.";
    public override string DefaultPrimaryColor => "#0F172A";
    public override string DefaultAccentColor => "#22D3EE";

    protected override void Layout(PageDescriptor page, InvoiceContext ctx)
    {
        page.Margin(0);
        page.Background().Svg(size => InvoiceBlocks.SidebarBackground(size, Side, ctx.P.Primary, ctx.P.Accent, Edge));

        var inv = ctx.Invoice;
        var on = ctx.P.OnPrimary;
        var total = InvoiceBlocks.Money(inv.Total);

        page.Content().PaddingLeft(Side + Edge + Gap).PaddingRight(36).PaddingTop(40).Column(col =>
        {
            // Drawn over the coloured column; it takes no room in the main flow.
            col.Item().Unconstrained().TranslateX(-(Side + Edge + Gap - 20)).Width(Side - 40).Column(s =>
            {
                s.Item().Element(c => InvoiceBlocks.LogoPlate(c, ctx, 46, on));
                if (ctx.Logo is not null)
                    s.Item().PaddingTop(10).Text(ctx.Issuer.Name).Bold().FontSize(11).FontColor(on);
                s.Item().PaddingTop(8).Column(c => InvoiceBlocks.ContactLines(c, ctx, on, 8));

                s.Item().PaddingTop(26).Element(c => InvoiceBlocks.Label(c, "Total due", on));
                s.Item().PaddingTop(3).Text(total).FontSize(InvoiceBlocks.FitSize(total, Side - 40, 17)).ExtraBold().FontColor(on);
                s.Item().PaddingTop(6).Column(c => InvoiceBlocks.SubtotalAndVat(c, ctx, on, 8.5f));

                s.Item().PaddingTop(24).Element(c => InvoiceBlocks.Label(c, "Payment details", on));
                s.Item().Column(c => InvoiceBlocks.PaymentFacts(c, ctx, on, on));
            });

            col.Item().Text("Invoice").FontSize(30).ExtraBold().FontColor(ctx.P.PrimaryText);
            col.Item().PaddingTop(10).Element(c => InvoiceBlocks.FactRow(c, ctx));
            col.Item().PaddingTop(22).Column(c => InvoiceBlocks.BillToClean(c, ctx));
            col.Item().PaddingTop(24).Element(c => InvoiceBlocks.ItemTable(c, ctx, Look));
            col.Item().PaddingTop(22).Column(c => InvoiceBlocks.TermsText(c, ctx));
            col.Item().PaddingTop(10).Width(200).ShowEntire().Element(c => InvoiceBlocks.Signature(c, ctx, 34));
        });
    }
}

/// <summary>"Callout": a two-part opening with the customer on one side and the amount due as a large typographic callout on the other.</summary>
public class CalloutInvoiceTemplate : InvoiceTemplateBase
{
    private static readonly TableLook Look = new(HeaderStyle.Underline, GridStyle.Rows, 9.5f, 4, 36f, 100f, 50f, 110f, 8f, 8f);

    public override string Key => InvoiceTemplateKeys.Callout;
    public override string Name => "Callout";
    public override string Description => "The customer on the left and a big amount-due figure on the right, then a crisp item list. Whitespace and type, no heavy blocks.";
    public override string DefaultPrimaryColor => "#7C3AED";
    public override string DefaultAccentColor => "#FB7185";

    protected override void Layout(PageDescriptor page, InvoiceContext ctx)
    {
        page.Margin(0);
        var inv = ctx.Invoice;
        var total = InvoiceBlocks.Money(inv.Total);

        page.Content().PaddingHorizontal(44).PaddingTop(40).Column(col =>
        {
            col.Item().Row(r =>
            {
                r.RelativeItem().Element(c => InvoiceBlocks.LogoOrName(c, ctx, 44, InvoiceBlocks.Side.Left, 17));
                r.ConstantItem(200).Column(c =>
                {
                    c.Item().AlignRight().Element(x => InvoiceBlocks.Label(x, "Invoice"));
                    c.Item().AlignRight().Text(inv.Reference).FontSize(13).Bold();
                });
            });

            col.Item().PaddingTop(34).Row(r =>
            {
                r.RelativeItem(3).Column(c =>
                {
                    InvoiceBlocks.BillToClean(c, ctx);
                    c.Item().PaddingTop(16).Row(f =>
                    {
                        f.RelativeItem().Column(x => InvoiceBlocks.Fact(x, "Issued", InvoiceBlocks.Day(inv.Date)));
                        f.RelativeItem().Column(x => InvoiceBlocks.Fact(x, "Due", InvoiceBlocks.Day(inv.DueDate)));
                    });
                });

                r.RelativeItem(2).PaddingLeft(24).Column(c =>
                {
                    c.Item().Element(x => InvoiceBlocks.Label(x, "Amount due", ctx.P.PrimaryText));
                    c.Item().PaddingTop(4).Text(total).FontSize(InvoiceBlocks.FitSize(total, 172, 30)).ExtraBold().FontColor(ctx.P.PrimaryText);
                    c.Item().PaddingTop(6).Width(70).Height(4).Background(ctx.P.Accent);
                    c.Item().PaddingTop(10).Column(x => InvoiceBlocks.SubtotalAndVat(x, ctx));
                });
            });

            col.Item().PaddingTop(32).Element(c => InvoiceBlocks.ItemTable(c, ctx, Look));
            InvoiceBlocks.PaymentAndSignatureClean(col, ctx, 30);
        });

        page.Footer().PaddingHorizontal(44).PaddingBottom(22).Column(f =>
        {
            f.Item().Height(0.8f).Background(InvoicePalette.RuleGrey);
            f.Item().PaddingTop(8).Element(c => InvoiceBlocks.FooterLine(c, ctx));
        });
    }
}
