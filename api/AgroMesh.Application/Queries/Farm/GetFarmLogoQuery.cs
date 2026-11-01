using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Services;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Queries.Farm;

public record GetFarmLogoQuery(string FarmId) : IRequest<FarmLogoFile>;

public class GetFarmLogoQueryHandler : IRequestHandler<GetFarmLogoQuery, FarmLogoFile>
{
    private readonly IFarmRepository _farmRepo;
    private readonly IStoredFileRepository _files;
    private readonly ICurrentUserAccessor _currentUser;

    public GetFarmLogoQueryHandler(IFarmRepository farmRepo, IStoredFileRepository files, ICurrentUserAccessor currentUser)
    {
        _farmRepo = farmRepo;
        _files = files;
        _currentUser = currentUser;
    }

    public async Task<FarmLogoFile> Handle(GetFarmLogoQuery query, CancellationToken ct)
    {
        var farm = await FarmAccess.GetFarmAsync(_farmRepo, _currentUser, query.FarmId, ct);
        if (farm.LogoFileId is null) throw new NotFoundException("Logo", query.FarmId);

        var file = await _files.GetByIdAsync(farm.LogoFileId, ct);
        if (file is null || file.FarmId != farm.Id) throw new NotFoundException("Logo", query.FarmId);

        return new FarmLogoFile(file.Content, file.ContentType, file.FileName, farm.LogoUpdatedAt);
    }
}
