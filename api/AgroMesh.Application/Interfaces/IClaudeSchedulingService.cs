namespace AgroMesh.Application.Interfaces;

public interface IClaudeSchedulingService
{
    Task RunSchedulingForFarmAsync(string farmId, CancellationToken ct = default);
    Task RunSchedulingForBlockAsync(string farmId, string blockId, CancellationToken ct = default);
}
