using QuestPDF.Drawing;

namespace AgroMesh.Infrastructure.Services.Invoices;

/// <summary>
/// Registers the Plus Jakarta Sans fonts that ship inside this assembly, once per process. They are embedded because the
/// production image has no system fonts, and the app (web and mobile) uses this typeface everywhere. The four weights
/// (Regular 400, SemiBold 600, Bold 700, ExtraBold 800) are static instances cut from the variable font the mobile app
/// uses: PDF rendering picks a weight by file, and cannot choose a weight inside a variable font.
/// </summary>
public static class InvoiceFonts
{
    public const string Family = "Plus Jakarta Sans";

    private static readonly object Gate = new();
    private static bool _registered;

    public static void EnsureRegistered()
    {
        if (_registered) return;
        lock (Gate)
        {
            if (_registered) return;

            var assembly = typeof(InvoiceFonts).Assembly;
            var names = assembly.GetManifestResourceNames().Where(n => n.Contains("PlusJakartaSans-", StringComparison.Ordinal) && n.EndsWith(".ttf", StringComparison.Ordinal)).ToList();
            if (names.Count == 0)
                throw new InvalidOperationException("The Plus Jakarta Sans fonts are missing from the AgroMesh.Infrastructure assembly.");

            foreach (var name in names)
            {
                using var stream = assembly.GetManifestResourceStream(name)!;
                FontManager.RegisterFontWithCustomName(Family, stream);
            }
            _registered = true;
        }
    }
}
