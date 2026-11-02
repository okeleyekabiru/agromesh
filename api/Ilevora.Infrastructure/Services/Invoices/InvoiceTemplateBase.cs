using Ilevora.Application.Interfaces;
using Ilevora.Application.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Ilevora.Infrastructure.Services.Invoices;

/// <summary>
/// The shared plumbing of every template: font registration, the logo (decoded once; an undrawable logo is dropped rather than
/// failing the invoice), the A4 page with Plus Jakarta Sans, and the resolved colours. A template only says how it lays the
/// invoice out (<see cref="Layout"/>) and which colours it uses when nobody chose any.
/// </summary>
public abstract class InvoiceTemplateBase : IInvoiceTemplateRenderer
{
    public abstract string Key { get; }
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string DefaultPrimaryColor { get; }
    public abstract string DefaultAccentColor { get; }

    /// <summary>The base text size of the template (pt).</summary>
    protected virtual float BaseFontSize => 9.5f;

    /// <summary>Arranges the page: set margins, content and footer on <paramref name="page"/> using the building blocks in <see cref="InvoiceBlocks"/>.</summary>
    protected abstract void Layout(PageDescriptor page, InvoiceContext ctx);

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

    public Document Compose(InvoiceRenderModel model, Image? logo)
    {
        InvoiceFonts.EnsureRegistered();
        var theme = model.Theme ?? new InvoiceTheme(DefaultPrimaryColor, DefaultAccentColor);
        var ctx = new InvoiceContext(model.Invoice, model.Issuer, new InvoicePalette(theme), logo);

        return Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.DefaultTextStyle(t => t.FontFamily(InvoiceFonts.Family).FontSize(BaseFontSize).FontColor(InvoicePalette.Ink));
            Layout(page, ctx);
            // Brand footer on every template. Always shown for now: there is no plan/setting concept to hide it behind yet.
            page.Foreground().AlignBottom().AlignCenter().PaddingBottom(2)
                .Text("Powered by Ilevora").FontSize(6).FontColor(InvoicePalette.Stone);
        }));
    }

    private static Image? TryLoadLogo(byte[]? bytes)
    {
        if (bytes is null || UploadedFileValidator.Sniff(bytes) is null or UploadKind.Pdf) return null;
        try { return Image.FromBinaryData(bytes); }
        catch (Exception) { return null; }
    }
}
