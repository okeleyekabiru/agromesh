using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AgroMesh.Infrastructure.Persistence.Repositories;

public class SchedulingPredictionRepository : ISchedulingPredictionRepository
{
    private readonly AgroMeshDbContext _db;

    public SchedulingPredictionRepository(AgroMeshDbContext db) => _db = db;

    public async Task<List<SchedulingPrediction>> GetByFarmIdAsync(string farmId, CancellationToken ct = default) =>
        await _db.SchedulingPredictions
            .Where(p => p.FarmId == farmId)
            .OrderBy(p => p.PredictedNextDue)
            .ToListAsync(ct);

    public async Task<SchedulingPrediction?> GetByBlockAndTypeAsync(
        string blockId, ActivityType activityType, CancellationToken ct = default) =>
        await _db.SchedulingPredictions
            .FirstOrDefaultAsync(p => p.BlockId == blockId && p.ActivityType == activityType, ct);

    public async Task UpsertAsync(SchedulingPrediction prediction, CancellationToken ct = default)
    {
        var existing = await GetByBlockAndTypeAsync(prediction.BlockId, prediction.ActivityType, ct);
        if (existing != null)
        {
            existing.Refresh(
                prediction.PredictedNextDue,
                prediction.Reasoning,
                prediction.Confidence,
                prediction.WeatherDataUsed,
                prediction.LastActivityDate,
                prediction.RecommendedChemical,
                prediction.RecommendedSprayDate);
            _db.SchedulingPredictions.Update(existing);
        }
        else
        {
            _db.SchedulingPredictions.Add(prediction);
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpsertManyAsync(List<SchedulingPrediction> predictions, CancellationToken ct = default)
    {
        foreach (var prediction in predictions)
            await UpsertAsync(prediction, ct);
    }
}
