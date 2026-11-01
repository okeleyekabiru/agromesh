using AgroMesh.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AgroMesh.Infrastructure.Services.Invoices;

/// <summary>
/// "NGF - Invoice": the Nature's Gold Farm layout. A4 portrait; logo and INVOICE title top right, BILLING TO top left;
/// an item table whose header is an accent tab (S/N, description) beside a primary bar (rate, qty, amount); subtotal, VAT and
/// a TOTAL band; payment method and terms at the left, signature line at the right; contact rows and three decorative
/// lines in the footer. Its default colours are the Nature's Gold Farm green and yellow; an invoice or the farm can override them.
/// </summary>
public class NgfInvoiceTemplate : InvoiceTemplateBase
{
    public const string Green = "#138A44";
    public const string Yellow = "#F5C90C";

    private static readonly TableLook Look = new(HeaderStyle.NgfSlant, GridStyle.Full);

    public override string Key => InvoiceTemplateKeys.Ngf;
    public override string Name => "NGF - Invoice";
    public override string Description => "Nature's Gold Farm: logo top right, slanted item-table header, TOTAL band and contact footer with decorative lines.";
    public override string DefaultPrimaryColor => Green;
    public override string DefaultAccentColor => Yellow;

    // Kept for the tests and any caller that formats money the way the invoices do.
    public static string Money(decimal value) => InvoiceBlocks.Money(value);
    public static string VatLabel(decimal percent) => InvoiceBlocks.VatLabel(percent);

    protected override void Layout(PageDescriptor page, InvoiceContext ctx)
    {
        page.Margin(0);

        page.Content().PaddingHorizontal(40).PaddingTop(34).Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c => InvoiceBlocks.BillTo(c, ctx));

                row.ConstantItem(220).Column(c =>
                {
                    c.Item().Element(x => InvoiceBlocks.LogoOrName(x, ctx, 66, InvoiceBlocks.Side.Right));
                    c.Item().PaddingTop(14).AlignRight().Text("INVOICE").FontSize(26).ExtraBold().FontColor(ctx.P.PrimaryText);
                    c.Item().PaddingTop(6).Column(m => InvoiceBlocks.Meta(m, ctx));
                });
            });

            col.Item().PaddingTop(28).Element(c => InvoiceBlocks.ItemTable(c, ctx, Look));
            col.Item().PaddingTop(14).Element(c => InvoiceBlocks.Totals(c, ctx, TotalStyle.NgfBand, Look.Rate + Look.Qty + Look.Amount + 60));
            InvoiceBlocks.PaymentTermsSignature(col, ctx);
        });

        page.Footer().PaddingTop(10).Row(row =>
        {
            row.RelativeItem().PaddingLeft(40).PaddingBottom(16).AlignBottom().Column(c =>
                InvoiceBlocks.IconRows(c, ctx, IconStyle.Primary, InvoicePalette.Ink));

            row.ConstantItem(190).Height(78).AlignBottom().Svg(size => InvoiceBlocks.DecorativeLines(size, ctx.P.Primary, ctx.P.Accent));
        });
    }
}
