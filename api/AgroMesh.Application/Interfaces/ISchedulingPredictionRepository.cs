using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;

namespace AgroMesh.Application.Interfaces;

public interface ISchedulingPredictionRepository
{
    Task<List<SchedulingPrediction>> GetByFarmIdAsync(string farmId, CancellationToken ct = default);
    Task<SchedulingPrediction?> GetBySectionAndTypeAsync(string sectionId, ActivityType activityType, CancellationToken ct = default);
    Task UpsertAsync(SchedulingPrediction prediction, CancellationToken ct = default);
    Task UpsertManyAsync(List<SchedulingPrediction> predictions, CancellationToken ct = default);
}
