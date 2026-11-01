using AgroMesh.Domain.Entities;

namespace AgroMesh.Application.Interfaces;

public record InvoiceTemplateInfo(string Key, string Name, string Description);

/// <summary>Who issues the invoice, as printed in its header and footer. Logo is the farm logo image bytes, if it has one.</summary>
public record InvoiceIssuer(string Name, string? Address, string? Phone, string? Email, string? Website, byte[]? Logo);

public record InvoiceRenderModel(Invoice Invoice, InvoiceIssuer Issuer);

/// <summary>
/// The invoice layouts the app can produce. Each template is one renderer class in Infrastructure registered in DI under its
/// own key, so adding a layout never touches the invoice entity, handlers or controllers.
/// </summary>
public interface IInvoiceTemplateRegistry
{
    IReadOnlyList<InvoiceTemplateInfo> Templates { get; }
    bool Exists(string key);
    /// <summary>Renders the invoice to a PDF with the template registered under <paramref name="key"/>.</summary>
    byte[] Render(string key, InvoiceRenderModel model);
}
