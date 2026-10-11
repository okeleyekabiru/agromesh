using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Constants;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.Utilities;
using AgroMesh.Domain.ValueObjects;
using MediatR;

namespace AgroMesh.Application.Commands.Activity;

public record LogActivityCommand(string FarmId, LogActivityRequest Request) : IRequest<LogActivityResponse>;

public class LogActivityCommandHandler : IRequestHandler<LogActivityCommand, LogActivityResponse>
{
    private readonly IActivityRepository _activityRepo;
    private readonly IBlockRepository _blockRepo;
    private readonly IFarmRepository _farmRepo;
    private readonly IWorkerRepository _workerRepo;
    private readonly IInventoryDeductionService _inventoryDeduction;
    private readonly IBackgroundJobQueue _jobQueue;

    public LogActivityCommandHandler(
        IActivityRepository activityRepo,
        IBlockRepository blockRepo,
        IFarmRepository farmRepo,
        IWorkerRepository workerRepo,
        IInventoryDeductionService inventoryDeduction,
        IBackgroundJobQueue jobQueue)
    {
        _activityRepo = activityRepo;
        _blockRepo = blockRepo;
        _farmRepo = farmRepo;
        _workerRepo = workerRepo;
        _inventoryDeduction = inventoryDeduction;
        _jobQueue = jobQueue;
    }

    public async Task<LogActivityResponse> Handle(LogActivityCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var block = await _blockRepo.GetByIdAsync(req.BlockId, ct)
            ?? throw new DomainException($"Block '{req.BlockId}' not found.");
        var farm = await _farmRepo.GetByIdAsync(command.FarmId, ct)
            ?? throw new DomainException($"Farm '{command.FarmId}' not found.");

        // Every activity records who did the work and what stock it used.
        if (req.Workers is null || req.Workers.Count == 0)
            throw new DomainException("Add at least one worker to the activity.");
        if (req.Inputs is null || req.Inputs.Count == 0)
            throw new DomainException("Add at least one stock item to the activity.");

        var workerLogs = new List<WorkerLog>();
        foreach (var w in req.Workers)
        {
            var worker = await _workerRepo.GetByIdWithRoleAsync(w.WorkerId, ct)
                ?? throw new DomainException($"Worker '{w.WorkerId}' not found.");
            workerLogs.Add(new WorkerLog(worker.Id, worker.Name, worker.Type, w.HoursWorked, worker.Role.PayRate, worker.Role.PayFrequency));
        }

        // Every input is a real stock item. Validated BEFORE anything is saved, so an activity
        // that stock can't cover is rejected whole rather than half-recorded.
        var stockInputs = await _inventoryDeduction.ResolveAsync(command.FarmId, req.BlockId, req.Inputs ?? [], ct);
        foreach (var s in stockInputs)
        {
            if (!ActivityConstants.AllowsStockCategory(req.ActivityType, s.Item.Category?.Name))
                throw new DomainException($"'{s.Item.Name}' ({s.Item.Category?.Name}) cannot be used in a {req.ActivityType} activity.");
        }
        var inputs = stockInputs.Select(s => new InputUsed(s.Item.Name, (double)s.Quantity, s.Item.Unit, s.Item.Id)).ToList();

        double? workedHectares = req.WorkedAreaValue.HasValue
            ? AreaConverter.ToHectares(req.WorkedAreaValue.Value, farm.PreferredAreaUnit)
            : null;

        // The chemical used is whatever Chemical stock the activity consumed, so it is tracked by name
        // (this is what spray-interval scheduling reads) without needing a separate catalogue entry.
        string? chemicalProductName = null;
        if (ActivityConstants.ChemicalActivityTypes.Contains(req.ActivityType))
        {
            var used = stockInputs
                .Where(s => string.Equals(s.Item.Category?.Name, "Chemical", StringComparison.OrdinalIgnoreCase))
                .Select(s => s.Item.Name).Distinct().ToList();
            // Spraying needs the chemical: no chemical, no spray.
            if (used.Count == 0)
                throw new DomainException($"{req.ActivityType} needs a chemical — add a Chemical stock item as an input.");
            chemicalProductName = string.Join(", ", used);
        }
        if (!string.IsNullOrWhiteSpace(req.ChemicalProductId) && !ActivityConstants.ChemicalActivityTypes.Contains(req.ActivityType))
            throw new DomainException($"Activity type '{req.ActivityType}' does not support a chemical product.");

        var activity = Domain.Entities.ActivityLog.Create(
            command.FarmId, req.BlockId, req.ActivityType, req.Date,
            workerLogs, inputs, req.SupervisorId, req.Notes, req.CreatedOffline,
            req.WorkedAreaGeoJson, workedHectares, req.ChemicalProductId, chemicalProductName);

        // Input cost = quantity used x the item's current average cost per unit, saved on the activity
        // so later price changes never rewrite what this work cost.
        activity.SetInputCost(stockInputs.Sum(s => s.Quantity * s.Item.UnitCost));

        await _activityRepo.AddAsync(activity, ct);

        block.MarkActive(activity.Date); // activity.Date is normalised to UTC
        await _blockRepo.UpdateAsync(block, ct);

        var warnings = await _inventoryDeduction.ApplyAsync(command.FarmId, req.BlockId, activity.Id, stockInputs, ct);

        if (req.ActivityType == Domain.Enums.ActivityType.Weeding || req.ActivityType == Domain.Enums.ActivityType.Spraying)
            _jobQueue.EnqueueSchedulingRun(command.FarmId, req.BlockId);

        var dto = new ActivityLogDto(
            activity.Id, activity.FarmId, activity.BlockId,
            activity.ActivityType, activity.Date, activity.Notes, activity.SupervisorId,
            activity.LabourCost, activity.InputCost, activity.CreatedOffline, activity.SyncedAt,
            activity.WorkedAreaGeoJson,
            activity.WorkedHectares.HasValue ? AreaDto.From(activity.WorkedHectares.Value, farm.PreferredAreaUnit) : null,
            activity.ChemicalProductId, activity.ChemicalProductName,
            activity.Workers.Select(w => new WorkerLogDto(w.WorkerId, w.WorkerName, w.WorkerType, w.HoursWorked, w.PayRate, w.PayFrequency)).ToList(),
            activity.Inputs.Select(i => new InputUsedDto(i.Item, i.Quantity, i.Unit)).ToList());

        return new LogActivityResponse(dto, warnings);
    }
}
