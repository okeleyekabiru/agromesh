using AgroMesh.Domain.Exceptions;

namespace AgroMesh.Application.Services;

public enum UploadKind { Png, Jpeg, WebP, Pdf }

/// <summary>What one kind of upload accepts. The type is decided by the file's own bytes, never by its name.</summary>
public record UploadRules(long MaxBytes, UploadKind[] Allowed, string Label)
{
    public static readonly UploadRules Logo = new(2 * 1024 * 1024, [UploadKind.Png, UploadKind.Jpeg, UploadKind.WebP], "logo");
    public static readonly UploadRules VendorInvoice = new(10 * 1024 * 1024, [UploadKind.Pdf, UploadKind.Png, UploadKind.Jpeg], "invoice file");

    public string MaxLabel => MaxBytes % (1024 * 1024) == 0 ? $"{MaxBytes / (1024 * 1024)} MB" : $"{MaxBytes / 1024} KB";
}

public record ValidatedUpload(UploadKind Kind, string ContentType, string Extension);

/// <summary>
/// Validates an uploaded file by size and magic bytes. The declared Content-Type (which a client controls) must agree
/// with what the bytes really are, so a renamed executable or an SVG with script in it cannot be stored as an image.
/// Throws PayloadTooLargeException (413), UnsupportedFileTypeException (415) or DomainException (422, empty file).
/// </summary>
public static class UploadedFileValidator
{
    public static void ValidateLength(long length, UploadRules rules)
    {
        if (length <= 0) throw new DomainException("The uploaded file is empty.");
        if (length > rules.MaxBytes)
            throw new PayloadTooLargeException($"The {rules.Label} is too large. The limit is {rules.MaxLabel}.");
    }

    public static ValidatedUpload Validate(byte[] content, string? declaredContentType, UploadRules rules)
    {
        ValidateLength(content.LongLength, rules);

        var kind = Sniff(content);
        if (kind is null || !rules.Allowed.Contains(kind.Value))
            throw new UnsupportedFileTypeException($"The {rules.Label} must be one of: {string.Join(", ", rules.Allowed.Select(Describe))}.");

        var declared = NormaliseDeclared(declaredContentType);
        if (declared is not null && declared != kind.Value)
            throw new UnsupportedFileTypeException("The file's content does not match its declared type.");

        return new ValidatedUpload(kind.Value, ContentTypeOf(kind.Value), ExtensionOf(kind.Value));
    }

    public static string ContentTypeOf(UploadKind kind) => kind switch
    {
        UploadKind.Png => "image/png",
        UploadKind.Jpeg => "image/jpeg",
        UploadKind.WebP => "image/webp",
        _ => "application/pdf",
    };

    public static string ExtensionOf(UploadKind kind) => kind switch
    {
        UploadKind.Png => ".png",
        UploadKind.Jpeg => ".jpg",
        UploadKind.WebP => ".webp",
        _ => ".pdf",
    };

    private static string Describe(UploadKind kind) => kind switch
    {
        UploadKind.Png => "PNG",
        UploadKind.Jpeg => "JPEG",
        UploadKind.WebP => "WebP",
        _ => "PDF",
    };

    public static UploadKind? Sniff(ReadOnlySpan<byte> b)
    {
        if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47 && b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A)
            return UploadKind.Png;
        if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF)
            return UploadKind.Jpeg;
        if (b.Length >= 12 && b[..4].SequenceEqual("RIFF"u8) && b.Slice(8, 4).SequenceEqual("WEBP"u8))
            return UploadKind.WebP;
        if (b.Length >= 5 && b[..5].SequenceEqual("%PDF-"u8))
            return UploadKind.Pdf;
        return null;
    }

    // A missing or generic type is not a claim: the bytes decide. Anything else must name the same kind.
    private static UploadKind? NormaliseDeclared(string? declared)
    {
        if (string.IsNullOrWhiteSpace(declared)) return null;
        var type = declared.Split(';')[0].Trim().ToLowerInvariant();
        return type switch
        {
            "application/octet-stream" or "binary/octet-stream" => null,
            "image/png" => UploadKind.Png,
            "image/jpeg" or "image/jpg" or "image/pjpeg" => UploadKind.Jpeg,
            "image/webp" => UploadKind.WebP,
            "application/pdf" => UploadKind.Pdf,
            _ => throw new UnsupportedFileTypeException($"The content type '{type}' is not supported."),
        };
    }

    /// <summary>The name shown back to the user: no path, no control characters or quotes, a sane length.</summary>
    public static string SafeFileName(string? name, string fallback)
    {
        if (string.IsNullOrWhiteSpace(name)) return fallback;
        var leaf = name.Replace('\\', '/').Split('/').Last();
        var chars = leaf.Select(c => char.IsControl(c) || c == '"' ? '_' : c).ToArray();
        var clean = new string(chars).Trim();
        if (clean.Length == 0) return fallback;
        if (clean.Length > 150)
        {
            var ext = Path.GetExtension(clean);
            if (ext.Length > 10) ext = "";
            clean = clean[..(150 - ext.Length)] + ext;
        }
        return clean;
    }
}
