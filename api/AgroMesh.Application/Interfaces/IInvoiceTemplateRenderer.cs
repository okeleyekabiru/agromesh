namespace AgroMesh.Application.Interfaces;

/// <summary>
/// One invoice layout. To add a template: implement this, register it in DI (<c>AddSingleton&lt;IInvoiceTemplateRenderer, ...&gt;</c>),
/// and give it a key. Nothing else changes: the registry discovers it and the API lists it. The registry holds at most
/// <see cref="IInvoiceTemplateRegistry.Max"/> templates.
/// </summary>
public interface IInvoiceTemplateRenderer
{
    /// <summary>Stored on each invoice. Lowercase, stable: changing it orphans invoices that use it.</summary>
    string Key { get; }
    string Name { get; }
    string Description { get; }
    /// <summary>The colours used when neither the invoice nor the farm picked any ("#RRGGBB").</summary>
    string DefaultPrimaryColor { get; }
    string DefaultAccentColor { get; }
    /// <summary>Draws the invoice with <c>model.Theme</c> (or the defaults above when it is null).</summary>
    byte[] Render(InvoiceRenderModel model);
}
