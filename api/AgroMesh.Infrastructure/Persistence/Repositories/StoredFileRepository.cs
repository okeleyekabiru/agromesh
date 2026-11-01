using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgroMesh.Infrastructure.Persistence.Repositories;

public class StoredFileRepository : IStoredFileRepository
{
    private readonly AgroMeshDbContext _db;

    public StoredFileRepository(AgroMeshDbContext db) => _db = db;

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
