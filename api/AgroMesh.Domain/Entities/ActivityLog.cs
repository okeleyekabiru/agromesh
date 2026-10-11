using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.Utilities;
using AgroMesh.Domain.ValueObjects;

namespace AgroMesh.Domain.Entities;

public class ActivityLog : BaseEntity
{
    public string FarmId { get; private set; } = string.Empty;
    public string BlockId { get; private set; } = string.Empty;
    public ActivityType ActivityType { get; private set; }
    public DateTime Date { get; private set; }
    public string? Notes { get; private set; }
    public string SupervisorId { get; private set; } = string.Empty;
    public decimal LabourCost { get; private set; }
    public decimal InputCost { get; private set; }
    public bool CreatedOffline { get; private set; }
    public DateTime? SyncedAt { get; private set; }

    // Spatial fields — populated for SpatialActivityTypes
    public string? WorkedAreaGeoJson { get; private set; }
    public double? WorkedHectares { get; private set; }

    // Chemical fields — populated for ChemicalActivityTypes
    public string? ChemicalProductId { get; private set; }
    public string? ChemicalProductName { get; private set; }

    // JSON columns
    public List<WorkerLog> Workers { get; private set; } = [];
    public List<InputUsed> Inputs { get; private set; } = [];

    private ActivityLog() { }

    public static ActivityLog Create(
        string farmId,
        string blockId,
        ActivityType activityType,
        DateTime date,
        List<WorkerLog> workers,
        List<InputUsed> inputs,
        string supervisorId,
        string? notes = null,
        bool createdOffline = false,
        string? workedAreaGeoJson = null,
        double? workedHectares = null,
        string? chemicalProductId = null,
        string? chemicalProductName = null)
    {
        if (string.IsNullOrWhiteSpace(blockId))
            throw new DomainException("Every activity must be assigned to a block.");

        var log = new ActivityLog
        {
            FarmId = farmId,
            BlockId = blockId,
            ActivityType = activityType,
            Date = ToUtc(date),
            Workers = workers ?? [],
            Inputs = inputs ?? [],
            SupervisorId = supervisorId,
            Notes = notes,
            CreatedOffline = createdOffline,
            WorkedAreaGeoJson = workedAreaGeoJson,
            WorkedHectares = workedHectares,
            ChemicalProductId = chemicalProductId,
            ChemicalProductName = chemicalProductName
        };

        log.LabourCost = workers?.Sum(w =>
            PayRateConverter.ToDailyEquivalent(w.PayRate, w.PayFrequency) * (decimal)(w.HoursWorked / 8.0)) ?? 0;
        log.InputCost = 0; // calculated externally once inventory costs are known

        return log;
    }

    // Npgsql only writes UTC to 'timestamp with time zone'. A date-only value from the web form
    // arrives as Kind=Unspecified, so treat it as a UTC calendar date rather than failing the save.
    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };

    public void MarkSynced()
    {
        SyncedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetInputCost(decimal cost)
    {
        InputCost = cost;
        UpdatedAt = DateTime.UtcNow;
    }
}
