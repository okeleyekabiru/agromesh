using AgroMesh.Domain.Entities;

namespace AgroMesh.Application.Interfaces;

/// <summary>A template as offered to users. The default colours are what an invoice looks like when neither the invoice nor the farm chose any.</summary>
public record InvoiceTemplateInfo(string Key, string Name, string Description, string DefaultPrimaryColor, string DefaultAccentColor);

/// <summary>The two brand colours an invoice is drawn with, already resolved ("#RRGGBB"). Renderers never choose colours themselves.</summary>
public record InvoiceTheme(string Primary, string Accent);

/// <summary>Who issues the invoice, as printed in its header and footer. Logo is the farm logo image bytes, if it has one.</summary>
public record InvoiceIssuer(string Name, string? Address, string? Phone, string? Email, string? Website, byte[]? Logo);

/// <summary>What a renderer needs. When Theme is null the template draws with its own default colours.</summary>
public record InvoiceRenderModel(Invoice Invoice, InvoiceIssuer Issuer, InvoiceTheme? Theme = null);

/// <summary>
/// The invoice layouts the app can produce (at most <see cref="Max"/>). Each template is one renderer class in Infrastructure
/// registered in DI under its own key, so adding a layout never touches the invoice entity, handlers or controllers.
/// </summary>
public interface IInvoiceTemplateRegistry
{
    /// <summary>The most templates the app offers. Registering more fails at startup.</summary>
    const int Max = 10;

    /// <summary>In a stable order: the default template first, then by name.</summary>
    IReadOnlyList<InvoiceTemplateInfo> Templates { get; }
    bool Exists(string key);
    InvoiceTemplateInfo? Find(string key);
    /// <summary>Renders the invoice to a PDF with the template registered under <paramref name="key"/>.</summary>
    byte[] Render(string key, InvoiceRenderModel model);
}
