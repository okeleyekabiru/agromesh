using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;

namespace AgroMesh.Infrastructure.Persistence.Repositories;

public class CompanyRepository : ICompanyRepository
{
    private readonly AgroMeshDbContext _db;

    public CompanyRepository(AgroMeshDbContext db) => _db = db;

    public async Task<Company?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _db.Companies.FindAsync([id], ct);

    public async Task<Company> AddAsync(Company company, CancellationToken ct = default)
    {
        _db.Companies.Add(company);
        await _db.SaveChangesAsync(ct);
        return company;
    }
}
