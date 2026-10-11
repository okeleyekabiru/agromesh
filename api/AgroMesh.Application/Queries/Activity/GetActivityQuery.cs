using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Queries.Activity;

public record GetActivityQuery(string FarmId, string ActivityId) : IRequest<ActivityLogDto>;

public class GetActivityQueryHandler : IRequestHandler<GetActivityQuery, ActivityLogDto>
{
    private readonly IActivityRepository _activityRepo;
    private readonly IFarmRepository _farmRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public GetActivityQueryHandler(IActivityRepository activityRepo, IFarmRepository farmRepo, ICurrentUserAccessor currentUser)
    {
        _activityRepo = activityRepo;
        _farmRepo = farmRepo;
        _currentUser = currentUser;
    }

    public async Task<ActivityLogDto> Handle(GetActivityQuery query, CancellationToken ct)
    {
        var farm = await _farmRepo.GetByIdAsync(query.FarmId, ct)
            ?? throw new NotFoundException("Farm", query.FarmId);
        if (!string.IsNullOrEmpty(_currentUser.CompanyId) && farm.CompanyId != _currentUser.CompanyId)
            throw new NotFoundException("Farm", query.FarmId);

        // The id came from the client: it must belong to the farm in the route, not just exist.
        var activity = await _activityRepo.GetByIdAsync(query.ActivityId, ct);
        if (activity is null || activity.FarmId != query.FarmId)
            throw new NotFoundException("Activity", query.ActivityId);

        return ActivityDtoMapper.ToDto(activity, farm.PreferredAreaUnit);
    }
}
