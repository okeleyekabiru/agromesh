namespace AgroMesh.Application.Interfaces;

public interface IClaudeSchedulingService
{
    Task RunSchedulingForFarmAsync(string farmId, CancellationToken ct = default);
    Task RunSchedulingForSectionAsync(string farmId, string sectionId, CancellationToken ct = default);
}
