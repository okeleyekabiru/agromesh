using Ilevora.Domain.Entities;

namespace Ilevora.Application.Interfaces;

public interface ICropAllocationRuleRepository
{
    Task<List<CropAllocationRule>> GetByFarmIdAsync(string farmId, CancellationToken ct = default);
    Task AddBulkAsync(List<CropAllocationRule> rules, CancellationToken ct = default);
}
