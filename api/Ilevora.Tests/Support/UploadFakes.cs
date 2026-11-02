using Ilevora.Application.Interfaces;
using Ilevora.Domain.Entities;
using Ilevora.Domain.Enums;

namespace Ilevora.Tests.Support;

internal class FakeFarms : IFarmRepository
{
    public List<Farm> Farms = [];
    public int UpdateCount;
    public FakeFarms(params Farm[] farms) => Farms.AddRange(farms);

    public Task<Farm?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult(Farms.FirstOrDefault(f => f.Id == id));
    public Task<Farm?> GetByIdWithBlocksAsync(string id, CancellationToken ct = default) => GetByIdAsync(id, ct);
    public Task<List<Farm>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(Farms.ToList());
    public Task<List<Farm>> GetByOwnerIdAsync(string ownerId, CancellationToken ct = default) => Task.FromResult(Farms.Where(f => f.OwnerId == ownerId).ToList());
    public Task<List<Farm>> GetByCompanyIdAsync(string companyId, CancellationToken ct = default) => Task.FromResult(Farms.Where(f => f.CompanyId == companyId).ToList());
    public Task<Farm> AddAsync(Farm farm, CancellationToken ct = default) { Farms.Add(farm); return Task.FromResult(farm); }
    public bool FailOnUpdate;
    public Task UpdateAsync(Farm farm, CancellationToken ct = default)
    {
        if (FailOnUpdate) throw new InvalidOperationException("db down");
        UpdateCount++;
        return Task.CompletedTask;
    }
    public Task<bool> ExistsAsync(string id, CancellationToken ct = default) => Task.FromResult(Farms.Any(f => f.Id == id));
}

internal class FakeUser(UserRole role = UserRole.Admin, string? companyId = "co-1", string? userId = "user-1") : ICurrentUserAccessor
{
    public string? UserId => userId;
    public string? CompanyId => companyId;
    public UserRole Role => role;
}

internal class FakeFiles : IStoredFileRepository
{
    public Dictionary<string, StoredFile> Files = [];
    public List<string> Deleted = [];

    public Task<StoredFile?> GetByIdAsync(string id, CancellationToken ct = default) =>
        Task.FromResult(Files.TryGetValue(id, out var f) ? f : null);

    public Task<StoredFile> AddAsync(StoredFile file, CancellationToken ct = default)
    {
        Files[file.Id] = file;
        return Task.FromResult(file);
    }

    public Task DeleteAsync(string id, CancellationToken ct = default)
    {
        Files.Remove(id);
        Deleted.Add(id);
        return Task.CompletedTask;
    }
}

/// <summary>Reports a fixed size, or "undecodable" when Info is null.</summary>
internal class FakeInspector(ImageInfo? info = null) : IImageInspector
{
    public ImageInfo? Info { get; set; } = info ?? new ImageInfo(400, 200);
    public ImageInfo? Inspect(byte[] content) => Info;
}
