using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using MediatR;

namespace AgroMesh.Application.Queries.Reference;

public record GetStatesQuery : IRequest<List<StateDto>>;

public class GetStatesQueryHandler : IRequestHandler<GetStatesQuery, List<StateDto>>
{
    private readonly IStateRepository _stateRepo;

    public GetStatesQueryHandler(IStateRepository stateRepo) => _stateRepo = stateRepo;

    public async Task<List<StateDto>> Handle(GetStatesQuery query, CancellationToken ct)
    {
        var states = await _stateRepo.GetAllAsync(ct);
        return states.Select(s => new StateDto(s.Id, s.Name, s.Code)).ToList();
    }
}
