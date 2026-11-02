using Ilevora.Application.Interfaces;
using Ilevora.Domain.Entities;

namespace Ilevora.Infrastructure.Persistence.Repositories;

public class CompanyRepository : ICompanyRepository
{
    private readonly IlevoraDbContext _db;

    public CompanyRepository(IlevoraDbContext db) => _db = db;

    public async Task<Company?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _db.Companies.FindAsync([id], ct);

    public async Task<Company> AddAsync(Company company, CancellationToken ct = default)
    {
        _db.Companies.Add(company);
        await _db.SaveChangesAsync(ct);
        return company;
    }
}
