using Ilevora.Application.Interfaces;
using Ilevora.Domain.Entities;
using Ilevora.Domain.Utilities;

namespace Ilevora.Infrastructure.Services.Invoices;

/// <summary>
/// Finds the renderer for an invoice template key. Every IInvoiceTemplateRenderer in DI is a template, up to
/// <see cref="IInvoiceTemplateRegistry.Max"/>: more than that, a repeated key, or a default colour that is not a valid brand
/// colour stops the app from starting, so a bad template cannot reach users.
/// </summary>
public class InvoiceTemplateRegistry : IInvoiceTemplateRegistry
{
    private readonly Dictionary<string, IInvoiceTemplateRenderer> _byKey;

    public InvoiceTemplateRegistry(IEnumerable<IInvoiceTemplateRenderer> renderers)
    {
        var all = renderers.ToList();
        if (all.Count > IInvoiceTemplateRegistry.Max)
            throw new InvalidOperationException($"At most {IInvoiceTemplateRegistry.Max} invoice templates can be registered, but {all.Count} were.");

        _byKey = new Dictionary<string, IInvoiceTemplateRenderer>(StringComparer.OrdinalIgnoreCase);
        foreach (var renderer in all)
        {
            if (!BrandColor.IsHex(renderer.DefaultPrimaryColor) || !BrandColor.IsHex(renderer.DefaultAccentColor))
                throw new InvalidOperationException($"Invoice template '{renderer.Key}' must have default colours in the form #RRGGBB.");
            if (!_byKey.TryAdd(renderer.Key, renderer))
                throw new InvalidOperationException($"Two invoice templates use the key '{renderer.Key}'.");
        }

        Templates = _byKey.Values
            .OrderByDescending(r => r.Key.Equals(InvoiceTemplateKeys.Default, StringComparison.OrdinalIgnoreCase))
            .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.Key, StringComparer.Ordinal)
            .Select(r => new InvoiceTemplateInfo(r.Key, r.Name, r.Description, r.DefaultPrimaryColor.ToUpperInvariant(), r.DefaultAccentColor.ToUpperInvariant()))
            .ToList();
    }

    public IReadOnlyList<InvoiceTemplateInfo> Templates { get; }

    public bool Exists(string key) => _byKey.ContainsKey(key);

    public InvoiceTemplateInfo? Find(string key) =>
        _byKey.TryGetValue(key, out var r)
            ? Templates.First(t => string.Equals(t.Key, r.Key, StringComparison.OrdinalIgnoreCase))
            : null;

    public byte[] Render(string key, InvoiceRenderModel model)
    {
        if (!_byKey.TryGetValue(key, out var renderer))
            throw new ArgumentException($"No invoice template is registered under '{key}'.", nameof(key));
        return renderer.Render(model);
    }
}
