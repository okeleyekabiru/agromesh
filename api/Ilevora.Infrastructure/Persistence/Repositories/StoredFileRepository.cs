using Ilevora.Application.Interfaces;
using Ilevora.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ilevora.Infrastructure.Persistence.Repositories;

public class StoredFileRepository : IStoredFileRepository
{
    private readonly IlevoraDbContext _db;

    public StoredFileRepository(IlevoraDbContext db) => _db = db;

    public async Task<StoredFile?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _db.StoredFiles.FindAsync([id], ct);

    public async Task<StoredFile> AddAsync(StoredFile file, CancellationToken ct = default)
    {
        _db.StoredFiles.Add(file);
        await _db.SaveChangesAsync(ct);
        return file;
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default) =>
        await _db.StoredFiles.Where(f => f.Id == id).ExecuteDeleteAsync(ct);
}
