using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;

namespace AgroMesh.Infrastructure.Services.Invoices;

/// <summary>Finds the renderer for an invoice template key. Every IInvoiceTemplateRenderer in DI is a template.</summary>
public class InvoiceTemplateRegistry : IInvoiceTemplateRegistry
{
    private readonly Dictionary<string, IInvoiceTemplateRenderer> _byKey;

    public InvoiceTemplateRegistry(IEnumerable<IInvoiceTemplateRenderer> renderers)
    {
        _byKey = new Dictionary<string, IInvoiceTemplateRenderer>(StringComparer.OrdinalIgnoreCase);
        foreach (var renderer in renderers)
        {
            if (!_byKey.TryAdd(renderer.Key, renderer))
                throw new InvalidOperationException($"Two invoice templates use the key '{renderer.Key}'.");
        }

        Templates = _byKey.Values
            .OrderByDescending(r => r.Key.Equals(InvoiceTemplateKeys.Default, StringComparison.OrdinalIgnoreCase))
            .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .Select(r => new InvoiceTemplateInfo(r.Key, r.Name, r.Description))
            .ToList();
    }

    public IReadOnlyList<InvoiceTemplateInfo> Templates { get; }

    public bool Exists(string key) => _byKey.ContainsKey(key);

    public byte[] Render(string key, InvoiceRenderModel model)
    {
        if (!_byKey.TryGetValue(key, out var renderer))
            throw new ArgumentException($"No invoice template is registered under '{key}'.", nameof(key));
        return renderer.Render(model);
    }
}
