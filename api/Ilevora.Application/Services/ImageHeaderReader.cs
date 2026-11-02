using Ilevora.Application.Interfaces;

namespace Ilevora.Application.Services;

/// <summary>
/// Reads the pixel size of a PNG, JPEG or WebP from its header without decoding it, so an image that is enormous when
/// decoded (a small file that expands to hundreds of megabytes) can be refused before anything tries to decode it.
/// Returns null when the header is truncated or does not make sense.
/// </summary>
public static class ImageHeaderReader
{
    public static ImageInfo? Read(byte[] b, UploadKind kind) => kind switch
    {
        UploadKind.Png => Png(b),
        UploadKind.Jpeg => Jpeg(b),
        UploadKind.WebP => WebP(b),
        _ => null,
    };


    /// <summary>
    /// A cheap structural check that the file is not cut short: a PNG has to reach its IEND chunk through well-formed chunks,
    /// a JPEG has to end with its end-of-image marker, and a WebP has to be as long as its RIFF header says. Catches a
    /// truncated upload whose header still looks fine, without decoding it.
    /// </summary>
    public static bool IsComplete(byte[] b, UploadKind kind) => kind switch
    {
        UploadKind.Png => PngComplete(b),
        UploadKind.Jpeg => JpegComplete(b),
        UploadKind.WebP => b.Length >= 12 && ((long)(b[4] | b[5] << 8 | b[6] << 16) + ((long)b[7] << 24)) + 8 <= b.Length,
        _ => false,
    };

    private static bool PngComplete(byte[] b)
    {
        long pos = 8;
        var sawHeader = false;
        var sawData = false;
        while (pos + 12 <= b.Length)
        {
            long length = (long)b[pos] << 24 | (long)b[pos + 1] << 16 | (long)b[pos + 2] << 8 | b[pos + 3];
            var type = System.Text.Encoding.ASCII.GetString(b, (int)pos + 4, 4);
            var next = pos + 12 + length;
            if (next > b.Length) return false;
            if (!sawHeader) { if (type != "IHDR") return false; sawHeader = true; }
            else if (type == "IDAT") sawData = true;
            else if (type == "IEND") return sawData;
            pos = next;
        }
        return false;
    }

    private static bool JpegComplete(byte[] b)
    {
        var i = b.Length - 1;
        var padding = 0;
        while (i > 0 && b[i] == 0 && padding < 16) { i--; padding++; }
        return i >= 1 && b[i] == 0xD9 && b[i - 1] == 0xFF;
    }

    private static ImageInfo? Valid(long w, long h) =>
        w is > 0 and <= 65535 && h is > 0 and <= 65535 ? new ImageInfo((int)w, (int)h) : null;

    private static ImageInfo? Png(byte[] b)
    {
        // 8 byte signature, then the IHDR chunk: length(4) "IHDR" width(4) height(4)
        if (b.Length < 24 || !b.AsSpan(12, 4).SequenceEqual("IHDR"u8)) return null;
        long w = (long)b[16] << 24 | (long)b[17] << 16 | (long)b[18] << 8 | b[19];
        long h = (long)b[20] << 24 | (long)b[21] << 16 | (long)b[22] << 8 | b[23];
        return Valid(w, h);
    }

    private static ImageInfo? Jpeg(byte[] b)
    {
        var i = 2;
        while (i + 3 < b.Length)
        {
            if (b[i] != 0xFF) { i++; continue; }
            var marker = b[i + 1];
            if (marker == 0xFF) { i++; continue; }                                  // fill byte
            if (marker is 0x01 or (>= 0xD0 and <= 0xD8)) { i += 2; continue; }      // markers without a length
            if (marker == 0xD9 || marker == 0xDA) return null;                      // end of image / start of scan before any frame header
            if (i + 4 > b.Length) return null;
            var length = b[i + 2] << 8 | b[i + 3];
            var isFrameHeader = marker is >= 0xC0 and <= 0xCF && marker is not (0xC4 or 0xC8 or 0xCC);
            if (isFrameHeader)
            {
                if (i + 9 > b.Length) return null;
                var h = b[i + 5] << 8 | b[i + 6];
                var w = b[i + 7] << 8 | b[i + 8];
                return Valid(w, h);
            }
            if (length < 2) return null;
            i += 2 + length;
        }
        return null;
    }

    private static ImageInfo? WebP(byte[] b)
    {
        if (b.Length < 30) return null;
        var chunk = System.Text.Encoding.ASCII.GetString(b, 12, 4);
        switch (chunk)
        {
            case "VP8 ":
                // frame tag(3) start code 9D 01 2A, then 14 bit width and height
                if (b[23] != 0x9D || b[24] != 0x01 || b[25] != 0x2A) return null;
                return Valid((b[26] | b[27] << 8) & 0x3FFF, (b[28] | b[29] << 8) & 0x3FFF);
            case "VP8L":
                if (b[20] != 0x2F) return null;
                var bits = (uint)(b[21] | b[22] << 8 | b[23] << 16 | b[24] << 24);
                return Valid((bits & 0x3FFF) + 1, ((bits >> 14) & 0x3FFF) + 1);
            case "VP8X":
                return Valid((b[24] | b[25] << 8 | b[26] << 16) + 1L, (b[27] | b[28] << 8 | b[29] << 16) + 1L);
            default:
                return null;
        }
    }
}
