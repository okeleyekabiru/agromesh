using AgroMesh.Domain.Exceptions;

namespace AgroMesh.Domain.Entities;

/// <summary>
/// An uploaded file (farm logo, vendor invoice scan) kept as bytes in the database. Nothing on a host like Render
/// survives a restart, so a file on disk would vanish; the bytes sit behind IStoredFileRepository so a bucket can replace
/// them later without touching the entities that point at a file by id. Content is never loaded for list queries.
/// </summary>
public class StoredFile : BaseEntity
{
    public string FarmId { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long Size { get; private set; }
    public byte[] Content { get; private set; } = [];

    private StoredFile() { }

    public static StoredFile Create(string farmId, string fileName, string contentType, byte[] content)
    {
        if (content is null || content.Length == 0)
            throw new DomainException("The uploaded file is empty.");

        return new StoredFile
        {
            FarmId = farmId,
            FileName = fileName,
            ContentType = contentType,
            Size = content.LongLength,
            Content = content,
        };
    }
}
