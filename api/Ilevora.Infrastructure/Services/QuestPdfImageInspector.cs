using Ilevora.Application.Interfaces;
using Ilevora.Application.Services;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Ilevora.Infrastructure.Services;

/// <summary>
/// Proves an upload is a real image: the size comes from the file header, and the picture is then decoded once by the
/// same library that will draw it on invoices (a one-image page), so a file that passes here cannot break a PDF later.
/// An image too big to be accepted is reported by size alone and never decoded.
/// </summary>
public class QuestPdfImageInspector : IImageInspector
{
    private const long MaxDecodedPixels = 4096L * 4096L;

    public ImageInfo? Inspect(byte[] content)
    {
        var kind = UploadedFileValidator.Sniff(content);
        if (kind is null or UploadKind.Pdf) return null;

        var info = ImageHeaderReader.Read(content, kind.Value);
        if (info is null || !ImageHeaderReader.IsComplete(content, kind.Value)) return null;
        if ((long)info.Width * info.Height > MaxDecodedPixels) return info;

        try
        {
            var image = Image.FromBinaryData(content);
            Document.Create(c => c.Page(p =>
            {
                p.Size(50, 50);
                p.Content().Image(image).FitArea();
            })).GeneratePdf();
            return info;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
