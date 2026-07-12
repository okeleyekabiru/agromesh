using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Queries.Activity;

public record GetActivitiesQuery(string FarmId, int Page = 1, int PageSize = 20) : IRequest<PagedResult<ActivityLogDto>>;

public class GetActivitiesQueryHandler : IRequestHandler<GetActivitiesQuery, PagedResult<ActivityLogDto>>
{
    private readonly IActivityRepository _activityRepo;
    private readonly IFarmRepository _farmRepo;

    public GetActivitiesQueryHandler(IActivityRepository activityRepo, IFarmRepository farmRepo)
    {
        _activityRepo = activityRepo;
        _farmRepo = farmRepo;
    }

    public async Task<PagedResult<ActivityLogDto>> Handle(GetActivitiesQuery query, CancellationToken ct)
    {
        var farm = await _farmRepo.GetByIdAsync(query.FarmId, ct)
            ?? throw new DomainException($"Farm '{query.FarmId}' not found.");

        var (items, total) = await _activityRepo.GetByFarmIdAsync(query.FarmId, query.Page, query.PageSize, ct);

        var dtos = items.Select(a => new ActivityLogDto(
            a.Id, a.FarmId, a.SectionId, a.BlockId, a.ActivityType, a.Date, a.Notes, a.SupervisorId,
            a.LabourCost, a.InputCost, a.CreatedOffline, a.SyncedAt, a.WorkedAreaGeoJson,
            a.WorkedHectares.HasValue ? AreaDto.From(a.WorkedHectares.Value, farm.PreferredAreaUnit) : null,
            a.ChemicalProductId, a.ChemicalProductName,
            a.Workers.Select(w => new WorkerLogDto(w.WorkerId, w.WorkerName, w.WorkerType, w.HoursWorked, w.PayRate, w.PayFrequency)).ToList(),
            a.Inputs.Select(i => new InputUsedDto(i.Item, i.Quantity, i.Unit)).ToList()
        )).ToList();

        return new PagedResult<ActivityLogDto>(dtos, total, query.Page, query.PageSize);
    }
}
