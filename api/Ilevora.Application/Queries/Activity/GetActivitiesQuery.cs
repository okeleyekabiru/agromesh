using Ilevora.Application.DTOs;
using Ilevora.Application.Interfaces;
using Ilevora.Domain.Exceptions;
using MediatR;

namespace Ilevora.Application.Queries.Activity;

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

        var dtos = items.Select(a => ActivityDtoMapper.ToDto(a, farm.PreferredAreaUnit)).ToList();

        return new PagedResult<ActivityLogDto>(dtos, total, query.Page, query.PageSize);
    }
}
