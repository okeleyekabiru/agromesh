using AgroMesh.Application.Services;
using AgroMesh.Infrastructure.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AgroMesh.Tests;

/// <summary>Real PNG/JPEG/WebP files produced by QuestPDF itself, so the readers are checked against genuine encoder output.</summary>
public class ImageInspectorTests
{
    static ImageInspectorTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    internal static byte[] Make(ImageFormat format, float width, float height)
    {
        var document = Document.Create(c => c.Page(p =>
        {
            p.Size(width, height);
            p.Content().Background(Colors.Green.Medium);
        }));
        return document.GenerateImages(new ImageGenerationSettings { ImageFormat = format, RasterDpi = 72 }).First();
    }

    private readonly QuestPdfImageInspector _inspector = new();

    [Theory]
    [InlineData(ImageFormat.Png, 120, 60)]
    [InlineData(ImageFormat.Jpeg, 200, 100)]
    [InlineData(ImageFormat.Webp, 90, 45)]
    public void RealImages_AreReadWithTheirSize(ImageFormat format, int width, int height)
    {
        var bytes = Make(format, width, height);

        var info = _inspector.Inspect(bytes);

        Assert.NotNull(info);
        Assert.Equal(width, info!.Width);
        Assert.Equal(height, info.Height);
    }

    [Theory]
    [InlineData(ImageFormat.Png)]
    [InlineData(ImageFormat.Jpeg)]
    [InlineData(ImageFormat.Webp)]
    public void MagicBytesAreRight_ForTheFormatsTheValidatorAccepts(ImageFormat format)
    {
        var kind = UploadedFileValidator.Sniff(Make(format, 50, 50));
        Assert.Equal(format switch { ImageFormat.Png => UploadKind.Png, ImageFormat.Jpeg => UploadKind.Jpeg, _ => UploadKind.WebP }, kind);
    }

    [Fact]
    public void PngWithItsPixelDataCutOff_IsNotAnImage()
    {
        var bytes = Make(ImageFormat.Png, 300, 300);
        var truncated = bytes[..40];
        Assert.NotNull(ImageHeaderReader.Read(truncated, UploadKind.Png)); // the header is fine: it is the decode that must fail
        Assert.Null(_inspector.Inspect(truncated));
    }

    [Fact]
    public void JpegWithItsScanCutOff_IsNotAnImage()
    {
        var bytes = Make(ImageFormat.Jpeg, 300, 300);
        // Keep the header (so the size can be read) but drop the picture data.
        var sof = Enumerable.Range(0, bytes.Length - 1).First(i => bytes[i] == 0xFF && bytes[i + 1] is 0xC0 or 0xC2);
        var truncated = bytes[..Math.Min(bytes.Length, sof + 30)];
        Assert.NotNull(ImageHeaderReader.Read(truncated, UploadKind.Jpeg));
        Assert.Null(_inspector.Inspect(truncated));
    }

    [Fact]
    public void PngSignatureFollowedByGarbage_IsNotAnImage()
    {
        var bytes = new byte[200];
        UploadedFileValidatorTests.Png.CopyTo(bytes, 0);
        new Random(1).NextBytes(bytes.AsSpan(8));
        Assert.Null(_inspector.Inspect(bytes));
    }

    [Fact]
    public void TextAndPdf_AreNotImages()
    {
        Assert.Null(_inspector.Inspect("hello"u8.ToArray()));
        Assert.Null(_inspector.Inspect(UploadedFileValidatorTests.Pdf));
    }

    [Fact]
    public void HugePixelCount_IsReportedWithoutBeingDecoded()
    {
        // A PNG header claiming 60000 x 60000 pixels, no pixel data: must come back as a size (so the caller refuses it), never be decoded.
        // Structurally complete (IHDR, IDAT, IEND; the CRCs are not checked) so it passes the completeness check.
        var bytes = new byte[61];
        UploadedFileValidatorTests.Png.AsSpan(0, 8).CopyTo(bytes);
        bytes[11] = 13;
        "IHDR"u8.CopyTo(bytes.AsSpan(12));
        bytes[16] = 0; bytes[17] = 0; bytes[18] = 0xEA; bytes[19] = 0x60;
        bytes[20] = 0; bytes[21] = 0; bytes[22] = 0xEA; bytes[23] = 0x60;
        bytes[36] = 4;
        "IDAT"u8.CopyTo(bytes.AsSpan(37));
        "IEND"u8.CopyTo(bytes.AsSpan(53));

        var info = _inspector.Inspect(bytes);

        Assert.NotNull(info);
        Assert.Equal(60000, info!.Width);
        Assert.Equal(60000, info.Height);
    }

    [Fact]
    public void ReaderRejectsAZeroSizedHeader()
    {
        var bytes = new byte[33];
        UploadedFileValidatorTests.Png.AsSpan(0, 8).CopyTo(bytes);
        "IHDR"u8.CopyTo(bytes.AsSpan(12));
        Assert.Null(ImageHeaderReader.Read(bytes, UploadKind.Png));
    }
}

public class ImageCompletenessTests
{
    private readonly QuestPdfImageInspector _inspector = new();

    private static byte[] PngHeaderClaiming(int w, int h, int extra)
    {
        var bytes = new byte[33 + extra];
        UploadedFileValidatorTests.Png.AsSpan(0, 8).CopyTo(bytes);
        bytes[11] = 13;
        "IHDR"u8.CopyTo(bytes.AsSpan(12));
        bytes[16] = (byte)(w >> 24); bytes[17] = (byte)(w >> 16); bytes[18] = (byte)(w >> 8); bytes[19] = (byte)w;
        bytes[20] = (byte)(h >> 24); bytes[21] = (byte)(h >> 16); bytes[22] = (byte)(h >> 8); bytes[23] = (byte)h;
        return bytes;
    }

    [Fact]
    public void TruncatedPng_WithAValid4096Header_IsNotAnImage()
    {
        Assert.Null(_inspector.Inspect(PngHeaderClaiming(4096, 4096, 0)));
        Assert.Null(_inspector.Inspect(PngHeaderClaiming(4096, 4096, 500)));
    }

    [Theory]
    [InlineData(ImageFormat.Png)]
    [InlineData(ImageFormat.Jpeg)]
    [InlineData(ImageFormat.Webp)]
    public void RealImages_AreComplete(ImageFormat format)
    {
        var bytes = ImageInspectorTests.Make(format, 80, 120);
        var kind = format switch { ImageFormat.Png => UploadKind.Png, ImageFormat.Jpeg => UploadKind.Jpeg, _ => UploadKind.WebP };
        Assert.True(ImageHeaderReader.IsComplete(bytes, kind));
        Assert.False(ImageHeaderReader.IsComplete(bytes[..(bytes.Length - 12)], kind));
    }

    [Theory]
    [InlineData(100, 4097)]
    [InlineData(4097, 100)]
    public void OversizeButValidPortraitOrLandscape_IsReportedBySizeNotRejectedAsUnreadable(int w, int h)
    {
        var info = _inspector.Inspect(ImageInspectorTests.Make(ImageFormat.Png, w, h));
        Assert.NotNull(info);
        Assert.Equal((w, h), (info!.Width, info.Height));
    }
}
