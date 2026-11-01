using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;

namespace AgroMesh.Application.Services;

/// <summary>Where each colour of an invoice came from, so a client can show "default" or "custom".</summary>
public record ThemeResolution(InvoiceTheme Theme, string PrimarySource, string AccentSource);

/// <summary>
/// The one rule for invoice colours, applied separately to the primary and the accent colour:
/// the invoice's own colour, else the farm's brand colour, else the template's default. An invoice stores only what its author
/// chose (null = not overridden), so changing the farm brand restyles every invoice that did not override it.
/// </summary>
public static class InvoiceThemeResolver
{
    public const string FromInvoice = "invoice";
    public const string FromFarm = "farm";
    public const string FromTemplate = "default";

    /// <summary>What an invoice whose template has gone is drawn with: the NGF colours.</summary>
    public static readonly InvoiceTheme Fallback = new("#138A44", "#F5C90C");

    public static ThemeResolution Resolve(string? invoicePrimary, string? invoiceAccent, Farm farm, InvoiceTemplateInfo? template)
    {
        var (primary, primarySource) = Pick(invoicePrimary, farm.BrandPrimaryColor, template?.DefaultPrimaryColor ?? Fallback.Primary);
        var (accent, accentSource) = Pick(invoiceAccent, farm.BrandAccentColor, template?.DefaultAccentColor ?? Fallback.Accent);
        return new ThemeResolution(new InvoiceTheme(primary, accent), primarySource, accentSource);
    }

    private static (string Colour, string Source) Pick(string? fromInvoice, string? fromFarm, string fromTemplate) =>
        !string.IsNullOrEmpty(fromInvoice) ? (fromInvoice, FromInvoice)
        : !string.IsNullOrEmpty(fromFarm) ? (fromFarm, FromFarm)
        : (fromTemplate, FromTemplate);
}
