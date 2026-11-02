using Ilevora.Domain.Entities;
using Ilevora.Domain.Enums;

namespace Ilevora.Application.Interfaces;

public interface ISchedulingPredictionRepository
{
    Task<List<SchedulingPrediction>> GetByFarmIdAsync(string farmId, CancellationToken ct = default);
    Task<SchedulingPrediction?> GetByBlockAndTypeAsync(string blockId, ActivityType activityType, CancellationToken ct = default);
    Task UpsertAsync(SchedulingPrediction prediction, CancellationToken ct = default);
    Task UpsertManyAsync(List<SchedulingPrediction> predictions, CancellationToken ct = default);
}
