namespace Ilevora.Application.Interfaces;

public record ImageInfo(int Width, int Height);

/// <summary>
/// Decodes an image to prove it really is one (the magic bytes only prove the first few bytes) and reports its size,
/// so a truncated or hostile file is refused when it is uploaded rather than when a PDF is rendered.
/// </summary>
public interface IImageInspector
{
    /// <summary>The image size, or null when the bytes cannot be decoded as an image.</summary>
    ImageInfo? Inspect(byte[] content);
}
