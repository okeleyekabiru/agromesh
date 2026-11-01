using AgroMesh.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AgroMesh.Infrastructure.Services.Invoices;

/// <summary>"Angle": a diagonal colour header with a parallel accent stripe, the logo and title riding on it, then a clean body.</summary>
public class AngleInvoiceTemplate : InvoiceTemplateBase
{
    private static readonly TableLook Look = new(HeaderStyle.Underline, GridStyle.Rows, 9.5f, 4, 36f, 100f, 50f, 110f, 8f, 8f);

    public override string Key => InvoiceTemplateKeys.Angle;
    public override string Name => "Angle";
    public override string Description => "A sweeping diagonal header with an accent stripe, carrying the logo and title, above a clean item list.";
    public override string DefaultPrimaryColor => "#DB2777";
    public override string DefaultAccentColor => "#FBBF24";

    protected override void Layout(PageDescriptor page, InvoiceContext ctx)
    {
        page.Margin(0);
        var inv = ctx.Invoice;

        page.Content().Column(col =>
        {
            col.Item().Height(212).Layers(layers =>
            {
                layers.Layer().Svg(size => InvoiceBlocks.AngledHeader(size, ctx.P.Primary, ctx.P.Accent, 190, 100));
                layers.PrimaryLayer().Height(212).PaddingHorizontal(44).PaddingTop(30).Row(r =>
                {
                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Element(x => InvoiceBlocks.LogoPlate(x, ctx, 48, ctx.P.OnPrimary));
                        if (ctx.Logo is not null)
                            c.Item().PaddingTop(8).Text(ctx.Issuer.Name).Bold().FontSize(11).FontColor(ctx.P.OnPrimary);
                    });
                    r.ConstantItem(240).Column(c =>
                    {
                        c.Item().AlignRight().Text("Invoice").FontSize(34).ExtraBold().FontColor(ctx.P.OnPrimary);
                        c.Item().AlignRight().Text(inv.Reference).FontSize(11).SemiBold().FontColor(ctx.P.OnPrimary);
                    });
                });
            });

            col.Item().PaddingHorizontal(44).PaddingTop(8).Column(body =>
            {
                body.Item().Row(r =>
                {
                    r.RelativeItem(3).Column(c => InvoiceBlocks.BillToClean(c, ctx));
                    r.RelativeItem(2).Column(c =>
                    {
                        InvoiceBlocks.Fact(c, "Issued", InvoiceBlocks.Day(inv.Date));
                        InvoiceBlocks.Fact(c, "Due", InvoiceBlocks.Day(inv.DueDate), top: 10);
                    });
                });
                body.Item().PaddingTop(20).Element(c => InvoiceBlocks.ItemTable(c, ctx, Look));
                body.Item().PaddingTop(14).Element(c => InvoiceBlocks.Totals(c, ctx, TotalStyle.Solid, 320));
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

/// <summary>"Anchor": a plain, airy page that ends in a full-width colour bar carrying subtotal, VAT and a highlighted total, pinned to the foot of the last page.</summary>
public class AnchorInvoiceTemplate : InvoiceTemplateBase
{
    private static readonly TableLook Look = new(HeaderStyle.Hairline, GridStyle.Zebra, 9.5f, 4, 36f, 100f, 50f, 110f, 8f, 8f);

    public override string Key => InvoiceTemplateKeys.Anchor;
    public override string Name => "Anchor";
    public override string Description => "An open layout that ends in a colour totals bar at the foot of the page, with a tinted contact strip below it.";
    public override string DefaultPrimaryColor => "#065F46";
    public override string DefaultAccentColor => "#A3E635";

    protected override void Layout(PageDescriptor page, InvoiceContext ctx)
    {
        page.Margin(0);
        var inv = ctx.Invoice;
        var total = InvoiceBlocks.Money(inv.Total);

        page.Content().Column(col =>
        {
            col.Item().PaddingHorizontal(44).PaddingTop(40).Column(body =>
            {
                body.Item().Row(r =>
                {
                    r.RelativeItem().Element(c => InvoiceBlocks.LogoOrName(c, ctx, 48, InvoiceBlocks.Side.Left, 17));
                    r.ConstantItem(220).Column(c =>
                    {
                        c.Item().AlignRight().Element(x => InvoiceBlocks.Label(x, "Invoice"));
                        c.Item().AlignRight().Text(inv.Reference).FontSize(24).ExtraBold().FontColor(ctx.P.PrimaryText);
                    });
                });
                body.Item().PaddingTop(30).Row(r =>
                {
                    r.RelativeItem(3).Column(c => InvoiceBlocks.BillToClean(c, ctx));
                    r.RelativeItem(2).Column(c =>
                    {
                        InvoiceBlocks.Fact(c, "Issued", InvoiceBlocks.Day(inv.Date));
                        InvoiceBlocks.Fact(c, "Due", InvoiceBlocks.Day(inv.DueDate), top: 10);
                    });
                });
                body.Item().PaddingTop(28).Element(c => InvoiceBlocks.ItemTable(c, ctx, Look));
                InvoiceBlocks.PaymentAndSignatureClean(body, ctx, 28);
                body.Item().Height(20);
            });

            // The last thing on the last page, pushed to its foot.
            col.Item().ExtendVertical().AlignBottom().ShowEntire().Background(ctx.P.Primary).PaddingHorizontal(44).PaddingVertical(20).Row(r =>
            {
                r.RelativeItem().Column(c => InvoiceBlocks.Fact(c, "Subtotal", InvoiceBlocks.Money(inv.Subtotal), ctx.P.OnPrimary, ctx.P.OnPrimary, 11));
                r.RelativeItem().Column(c => InvoiceBlocks.Fact(c, InvoiceBlocks.VatLabel(inv.VatPercent).Replace("TAX VAT", "VAT"), InvoiceBlocks.Money(inv.VatAmount), ctx.P.OnPrimary, ctx.P.OnPrimary, 11));
                r.ConstantItem(220).CornerRadius(10).Background(ctx.P.Accent).Padding(12).Column(c =>
                {
                    c.Item().Element(x => InvoiceBlocks.Label(x, "Total due", ctx.P.OnAccent));
                    c.Item().PaddingTop(2).Text(total).FontSize(InvoiceBlocks.FitSize(total, 190, 20)).ExtraBold().FontColor(ctx.P.OnAccent);
                });
            });
        });

        page.Footer().Background(ctx.P.PrimaryTint).PaddingHorizontal(44).PaddingVertical(9).AlignCenter()
            .Text(InvoiceBlocks.InlineContacts(ctx.Issuer)).FontSize(8).FontColor(InvoicePalette.Muted);
    }
}

/// <summary>"Duo": two coloured panels side by side under the title: the customer on the primary colour, the total due on the accent colour.</summary>
public class DuoInvoiceTemplate : InvoiceTemplateBase
{
    private static readonly TableLook Look = new(HeaderStyle.Hairline, GridStyle.Zebra, 9.5f, 4, 36f, 100f, 50f, 110f, 8f, 8f);

    public override string Key => InvoiceTemplateKeys.Duo;
    public override string Name => "Duo";
    public override string Description => "Two colour panels side by side: customer and dates on one, the amount due on the other, above a soft zebra item list.";
    public override string DefaultPrimaryColor => "#1D4ED8";
    public override string DefaultAccentColor => "#F97316";

    protected override void Layout(PageDescriptor page, InvoiceContext ctx)
    {
        page.Margin(0);
        var inv = ctx.Invoice;
        var total = InvoiceBlocks.Money(inv.Total);

        page.Content().PaddingHorizontal(44).PaddingTop(38).Column(col =>
        {
            col.Item().Row(r =>
            {
                r.RelativeItem().Element(c => InvoiceBlocks.LogoOrName(c, ctx, 48, InvoiceBlocks.Side.Left, 17));
                r.ConstantItem(220).Column(c =>
                {
                    c.Item().AlignRight().Text("Invoice").FontSize(28).ExtraBold().FontColor(ctx.P.PrimaryText);
                    c.Item().AlignRight().Text(inv.Reference).FontSize(11).SemiBold().FontColor(InvoicePalette.Muted);
                });
            });

            col.Item().PaddingTop(24).ShowEntire().Row(r =>
            {
                r.RelativeItem().Background(ctx.P.Primary).Padding(22).Column(c =>
                {
                    InvoiceBlocks.BillToClean(c, ctx, labelColor: ctx.P.OnPrimary, textColor: ctx.P.OnPrimary);
                    c.Item().PaddingTop(14).Row(f =>
                    {
                        f.RelativeItem().Column(x => InvoiceBlocks.Fact(x, "Issued", InvoiceBlocks.Day(inv.Date), ctx.P.OnPrimary, ctx.P.OnPrimary));
                        f.RelativeItem().Column(x => InvoiceBlocks.Fact(x, "Due", InvoiceBlocks.Day(inv.DueDate), ctx.P.OnPrimary, ctx.P.OnPrimary));
                    });
                });
                r.RelativeItem().Background(ctx.P.Accent).Padding(22).Column(c =>
                {
                    c.Item().Element(x => InvoiceBlocks.Label(x, "Total due", ctx.P.OnAccent));
                    c.Item().PaddingTop(4).Text(total).FontSize(InvoiceBlocks.FitSize(total, 200, 26)).ExtraBold().FontColor(ctx.P.OnAccent);
                    c.Item().PaddingTop(12).Column(x => InvoiceBlocks.SubtotalAndVat(x, ctx, ctx.P.OnAccent));
                });
            });

            col.Item().PaddingTop(26).Element(c => InvoiceBlocks.ItemTable(c, ctx, Look));
            InvoiceBlocks.PaymentAndSignatureClean(col, ctx, 30);
        });

        page.Footer().PaddingHorizontal(44).PaddingBottom(22).Column(f =>
        {
            f.Item().Height(3).Background(ctx.P.Accent);
            f.Item().PaddingTop(8).Element(c => InvoiceBlocks.FooterLine(c, ctx));
        });
    }
}
