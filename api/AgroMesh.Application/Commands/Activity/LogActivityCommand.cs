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
    private readonly ISectionRepository _sectionRepo;
    private readonly IFarmRepository _farmRepo;
    private readonly IWorkerRepository _workerRepo;
    private readonly IInventoryDeductionService _inventoryDeduction;
    private readonly IBackgroundJobQueue _jobQueue;

    public LogActivityCommandHandler(
        IActivityRepository activityRepo,
        ISectionRepository sectionRepo,
        IFarmRepository farmRepo,
        IWorkerRepository workerRepo,
        IInventoryDeductionService inventoryDeduction,
        IBackgroundJobQueue jobQueue)
    {
        _activityRepo = activityRepo;
        _sectionRepo = sectionRepo;
        _farmRepo = farmRepo;
        _workerRepo = workerRepo;
        _inventoryDeduction = inventoryDeduction;
        _jobQueue = jobQueue;
    }

    public async Task<LogActivityResponse> Handle(LogActivityCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var section = await _sectionRepo.GetByIdWithBlocksAsync(req.SectionId, ct)
            ?? throw new DomainException($"Section '{req.SectionId}' not found.");
        var farm = await _farmRepo.GetByIdAsync(command.FarmId, ct)
            ?? throw new DomainException($"Farm '{command.FarmId}' not found.");

        if (!section.Blocks.Any(b => b.Id == req.BlockId))
            throw new DomainException($"Block '{req.BlockId}' does not belong to section '{req.SectionId}'.");

        var workerLogs = new List<WorkerLog>();
        foreach (var w in req.Workers)
        {
            var worker = await _workerRepo.GetByIdWithRoleAsync(w.WorkerId, ct)
                ?? throw new DomainException($"Worker '{w.WorkerId}' not found.");
            workerLogs.Add(new WorkerLog(worker.Id, worker.Name, worker.Type, w.HoursWorked, worker.Role.PayRate, worker.Role.PayFrequency));
        }

        var inputs = req.Inputs.Select(i => new InputUsed(i.Item, i.Quantity, i.Unit)).ToList();

        double? workedHectares = req.WorkedAreaValue.HasValue
            ? AreaConverter.ToHectares(req.WorkedAreaValue.Value, farm.PreferredAreaUnit)
            : null;

        string? chemicalProductName = null;
        if (!string.IsNullOrWhiteSpace(req.ChemicalProductId) && !ActivityConstants.ChemicalActivityTypes.Contains(req.ActivityType))
            throw new DomainException($"Activity type '{req.ActivityType}' does not support a chemical product.");

        var activity = Domain.Entities.ActivityLog.Create(
            command.FarmId, req.SectionId, req.BlockId, req.ActivityType, req.Date,
            workerLogs, inputs, req.SupervisorId, req.Notes, req.CreatedOffline,
            req.WorkedAreaGeoJson, workedHectares, req.ChemicalProductId, chemicalProductName);

        await _activityRepo.AddAsync(activity, ct);

        section.MarkActive(req.Date);
        await _sectionRepo.UpdateAsync(section, ct);

        var warnings = await _inventoryDeduction.DeductAsync(command.FarmId, activity.Id, inputs, ct);

        if (req.ActivityType == Domain.Enums.ActivityType.Weeding || req.ActivityType == Domain.Enums.ActivityType.Spraying)
            _jobQueue.EnqueueSchedulingRun(command.FarmId, req.SectionId);

        var dto = new ActivityLogDto(
            activity.Id, activity.FarmId, activity.SectionId, activity.BlockId,
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
