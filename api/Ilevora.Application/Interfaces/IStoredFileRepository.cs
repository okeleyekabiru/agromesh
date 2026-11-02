using Ilevora.Domain.Entities;

namespace Ilevora.Application.Interfaces;

/// <summary>Where uploaded files live. Today that is a database table; the contract hides it so a bucket can replace it.</summary>
public interface IStoredFileRepository
{
    Task<StoredFile?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<StoredFile> AddAsync(StoredFile file, CancellationToken ct = default);
    /// <summary>Removes the file. Does nothing when it is already gone.</summary>
    Task DeleteAsync(string id, CancellationToken ct = default);
}
