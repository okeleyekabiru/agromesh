using System.Globalization;
using System.Text.RegularExpressions;
using Ilevora.Domain.Exceptions;

namespace Ilevora.Domain.Utilities;

/// <summary>
/// Brand colours for invoices: validation, normalisation and the readability maths. Only strict "#RRGGBB" is accepted (any case,
/// stored upper-case): no CSS names, shorthand, rgb() or alpha, so one value means one colour on every client and in the PDF.
/// </summary>
public static partial class BrandColor
{
    public const string White = "#FFFFFF";
    /// <summary>The dark text colour used on light fills.</summary>
    public const string Ink = "#1F2A24";

    /// <summary>A colour whose contrast against white paper is below this would vanish on the page, so it is refused.</summary>
    public const double MinContrastAgainstWhite = 1.25;

    /// <summary>Text and thin lines in a brand colour are darkened until they reach this contrast on white (WCAG large/bold text).</summary>
    public const double TextContrastAgainstWhite = 3.0;

    [GeneratedRegex(@"\A#[0-9A-Fa-f]{6}\z", RegexOptions.CultureInvariant)]
    private static partial Regex HexPattern();

    public static bool IsHex(string? value) => value is not null && HexPattern().IsMatch(value);

    /// <summary>Null for blank (meaning "no custom colour"); the upper-case hex for a valid colour; DomainException otherwise.</summary>
    public static string? Normalise(string? raw, string label)
    {
        var trimmed = raw?.Trim();
        if (string.IsNullOrEmpty(trimmed)) return null;
        if (!IsHex(trimmed))
            throw new DomainException($"{label} must be a colour in the form #RRGGBB (for example #138A44).");
        var hex = trimmed.ToUpperInvariant();
        if (Contrast(hex, White) < MinContrastAgainstWhite)
            throw new DomainException($"{label} {hex} is too light to show on white paper. Choose a darker colour.");
        return hex;
    }

    public static double Luminance(string hex)
    {
        var (r, g, b) = Rgb(hex);
        static double Lin(int c) { var s = c / 255.0; return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4); }
        return 0.2126 * Lin(r) + 0.7152 * Lin(g) + 0.0722 * Lin(b);
    }

    public static double Contrast(string a, string b)
    {
        var la = Luminance(a);
        var lb = Luminance(b);
        var (hi, lo) = la >= lb ? (la, lb) : (lb, la);
        return (hi + 0.05) / (lo + 0.05);
    }

    /// <summary>White or dark ink, whichever reads better on the given fill.</summary>
    public static string TextOn(string fill) => Contrast(White, fill) >= Contrast(Ink, fill) ? White : Ink;

    /// <summary>The colour to use for text and thin lines on white paper: itself if it already has 3:1 contrast, otherwise darkened step by step until it does.</summary>
    public static string ForText(string hex)
    {
        var current = hex;
        for (var i = 0; i < 40 && Contrast(current, White) < TextContrastAgainstWhite; i++)
            current = Mix(current, "#000000", 0.05);
        return current;
    }

    /// <summary>The colour mixed with white: amount 0 is the colour, 1 is white. For zebra rows and soft panels.</summary>
    public static string Tint(string hex, double amount) => Mix(hex, White, amount);

    private static string Mix(string from, string to, double amount)
    {
        var (r1, g1, b1) = Rgb(from);
        var (r2, g2, b2) = Rgb(to);
        int Lerp(int a, int b) => (int)Math.Round(a + (b - a) * amount);
        return $"#{Lerp(r1, r2):X2}{Lerp(g1, g2):X2}{Lerp(b1, b2):X2}";
    }

    private static (int R, int G, int B) Rgb(string hex) =>
        (int.Parse(hex.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
         int.Parse(hex.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
         int.Parse(hex.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
}
