namespace AgroMesh.Application.Interfaces;

/// <summary>
/// One invoice layout. To add a template: implement this, register it in DI (<c>AddScoped&lt;IInvoiceTemplateRenderer, ...&gt;</c>),
/// and give it a key. Nothing else changes: the registry discovers it and the API lists it.
/// </summary>
public interface IInvoiceTemplateRenderer
{
    /// <summary>Stored on each invoice. Lowercase, stable: changing it orphans invoices that use it.</summary>
    string Key { get; }
    string Name { get; }
    string Description { get; }
    byte[] Render(InvoiceRenderModel model);
}
