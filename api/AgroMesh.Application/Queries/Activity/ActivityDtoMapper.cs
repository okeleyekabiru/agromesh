using AgroMesh.Application.DTOs;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;

namespace AgroMesh.Application.Queries.Activity;

internal static class ActivityDtoMapper
{
    public static ActivityLogDto ToDto(ActivityLog a, AreaUnit unit) => new(
        a.Id, a.FarmId, a.BlockId, a.ActivityType, a.Date, a.Notes, a.SupervisorId,
        a.LabourCost, a.InputCost, a.CreatedOffline, a.SyncedAt, a.WorkedAreaGeoJson,
        a.WorkedHectares.HasValue ? AreaDto.From(a.WorkedHectares.Value, unit) : null,
        a.ChemicalProductId, a.ChemicalProductName,
        a.Workers.Select(w => new WorkerLogDto(w.WorkerId, w.WorkerName, w.WorkerType, w.HoursWorked, w.PayRate, w.PayFrequency)).ToList(),
        a.Inputs.Select(i => new InputUsedDto(i.Item, i.Quantity, i.Unit)).ToList());
}
