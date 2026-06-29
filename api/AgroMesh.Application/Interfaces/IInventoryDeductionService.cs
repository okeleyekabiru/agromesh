using AgroMesh.Domain.ValueObjects;

namespace AgroMesh.Application.Interfaces;

public interface IInventoryDeductionService
{
    Task<List<string>> DeductAsync(string farmId, string activityLogId, List<InputUsed> inputs, CancellationToken ct = default);
}
