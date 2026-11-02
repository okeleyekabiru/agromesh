using Ilevora.Application.Services;
using Ilevora.Domain.Exceptions;

namespace Ilevora.Tests;

public class UploadedFileValidatorTests
{
    internal static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0];
    internal static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10, 0x4A, 0x46];
    internal static readonly byte[] Pdf = "%PDF-1.7\n1 0 obj"u8.ToArray();
    internal static readonly byte[] Gif = "GIF89a......"u8.ToArray();
    internal static readonly byte[] Svg = "<svg xmlns='http://www.w3.org/2000/svg'></svg>"u8.ToArray();
    internal static byte[] WebP()
    {
        var b = new byte[16];
        "RIFF"u8.CopyTo(b.AsSpan(0));
        "WEBP"u8.CopyTo(b.AsSpan(8));
        "VP8 "u8.CopyTo(b.AsSpan(12));
        return b;
    }

    [Theory]
    [InlineData(UploadKind.Png, "image/png", ".png")]
    [InlineData(UploadKind.Jpeg, "image/jpeg", ".jpg")]
    [InlineData(UploadKind.WebP, "image/webp", ".webp")]
    public void Logo_AcceptsPngJpegWebP(UploadKind kind, string contentType, string ext)
    {
        var bytes = kind switch { UploadKind.Png => Png, UploadKind.Jpeg => Jpeg, _ => WebP() };
        var result = UploadedFileValidator.Validate(bytes, contentType, UploadRules.Logo);
        Assert.Equal(kind, result.Kind);
        Assert.Equal(contentType, result.ContentType);
        Assert.Equal(ext, result.Extension);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("application/octet-stream")]
    public void DeclaredType_WhenMissingOrGeneric_IsIgnoredAndBytesDecide(string? declared)
    {
        var result = UploadedFileValidator.Validate(Png, declared, UploadRules.Logo);
        Assert.Equal("image/png", result.ContentType);
    }

    [Fact]
    public void DeclaredType_JpgAlias_IsAccepted()
    {
        var result = UploadedFileValidator.Validate(Jpeg, "image/jpg", UploadRules.Logo);
        Assert.Equal(UploadKind.Jpeg, result.Kind);
    }

    [Fact]
    public void DeclaredType_ThatDisagreesWithBytes_Is415()
    {
        Assert.Throws<UnsupportedFileTypeException>(() => UploadedFileValidator.Validate(Png, "image/jpeg", UploadRules.Logo));
    }

    [Fact]
    public void DeclaredType_WithCharsetParameter_IsStillMatched()
    {
        var result = UploadedFileValidator.Validate(Pdf, "application/pdf; charset=binary", UploadRules.VendorInvoice);
        Assert.Equal(UploadKind.Pdf, result.Kind);
    }

    [Fact]
    public void Logo_RejectsPdf_Gif_Svg_Text()
    {
        Assert.Throws<UnsupportedFileTypeException>(() => UploadedFileValidator.Validate(Pdf, "application/pdf", UploadRules.Logo));
        Assert.Throws<UnsupportedFileTypeException>(() => UploadedFileValidator.Validate(Gif, "image/gif", UploadRules.Logo));
        Assert.Throws<UnsupportedFileTypeException>(() => UploadedFileValidator.Validate(Svg, "image/svg+xml", UploadRules.Logo));
        Assert.Throws<UnsupportedFileTypeException>(() => UploadedFileValidator.Validate("hello world, not an image"u8.ToArray(), "image/png", UploadRules.Logo));
    }

    [Fact]
    public void RiffThatIsNotWebP_IsRejected()
    {
        var wav = new byte[16];
        "RIFF"u8.CopyTo(wav.AsSpan(0));
        "WAVE"u8.CopyTo(wav.AsSpan(8));
        Assert.Throws<UnsupportedFileTypeException>(() => UploadedFileValidator.Validate(wav, "image/webp", UploadRules.Logo));
    }

    [Fact]
    public void VendorInvoice_AcceptsPdfPngJpeg_RejectsWebPAndGif()
    {
        Assert.Equal(UploadKind.Pdf, UploadedFileValidator.Validate(Pdf, "application/pdf", UploadRules.VendorInvoice).Kind);
        Assert.Equal(UploadKind.Png, UploadedFileValidator.Validate(Png, "image/png", UploadRules.VendorInvoice).Kind);
        Assert.Equal(UploadKind.Jpeg, UploadedFileValidator.Validate(Jpeg, "image/jpeg", UploadRules.VendorInvoice).Kind);
        Assert.Throws<UnsupportedFileTypeException>(() => UploadedFileValidator.Validate(WebP(), "image/webp", UploadRules.VendorInvoice));
        Assert.Throws<UnsupportedFileTypeException>(() => UploadedFileValidator.Validate(Gif, "image/gif", UploadRules.VendorInvoice));
    }

    [Fact]
    public void ExecutableRenamedToPdf_IsRejected()
    {
        var exe = new byte[] { 0x4D, 0x5A, 0x90, 0, 3, 0, 0, 0 };
        Assert.Throws<UnsupportedFileTypeException>(() => UploadedFileValidator.Validate(exe, "application/pdf", UploadRules.VendorInvoice));
    }

    [Fact]
    public void EmptyFile_IsRejectedWithDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => UploadedFileValidator.Validate([], "image/png", UploadRules.Logo));
        Assert.IsNotType<UnsupportedFileTypeException>(ex);
        Assert.Contains("empty", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Logo_OverTwoMegabytes_Is413()
    {
        var big = new byte[2 * 1024 * 1024 + 1];
        Png.CopyTo(big, 0);
        Assert.Throws<PayloadTooLargeException>(() => UploadedFileValidator.Validate(big, "image/png", UploadRules.Logo));
    }

    [Fact]
    public void Logo_ExactlyTwoMegabytes_IsAccepted()
    {
        var exact = new byte[2 * 1024 * 1024];
        Png.CopyTo(exact, 0);
        Assert.Equal(UploadKind.Png, UploadedFileValidator.Validate(exact, "image/png", UploadRules.Logo).Kind);
    }

    [Fact]
    public void VendorInvoice_OverTenMegabytes_Is413()
    {
        var big = new byte[10 * 1024 * 1024 + 1];
        Pdf.CopyTo(big, 0);
        Assert.Throws<PayloadTooLargeException>(() => UploadedFileValidator.Validate(big, "application/pdf", UploadRules.VendorInvoice));
    }

    [Fact]
    public void ValidateSize_ChecksLengthBeforeReadingBytes()
    {
        Assert.Throws<PayloadTooLargeException>(() => UploadedFileValidator.ValidateLength(UploadRules.Logo.MaxBytes + 1, UploadRules.Logo));
        Assert.Throws<DomainException>(() => UploadedFileValidator.ValidateLength(0, UploadRules.Logo));
        UploadedFileValidator.ValidateLength(1, UploadRules.Logo);
    }

    [Theory]
    [InlineData(@"C:\fakepath\My Logo.PNG", "My Logo.PNG")]
    [InlineData("C:/fakepath/My Logo.PNG", "My Logo.PNG")]
    [InlineData("../../etc/passwd", "passwd")]
    [InlineData("  ", "file")]
    [InlineData(null, "file")]
    [InlineData("a\r\nb\".pdf", "a__b_.pdf")]
    public void SafeFileName_StripsPathsAndControlCharacters(string? input, string expected)
    {
        Assert.Equal(expected, UploadedFileValidator.SafeFileName(input, "file"));
    }

    [Fact]
    public void SafeFileName_IsCappedAt150Characters()
    {
        var name = new string('a', 400) + ".pdf";
        Assert.True(UploadedFileValidator.SafeFileName(name, "file").Length <= 150);
        Assert.EndsWith(".pdf", UploadedFileValidator.SafeFileName(name, "file"));
    }
}
