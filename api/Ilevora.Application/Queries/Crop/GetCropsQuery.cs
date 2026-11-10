using Ilevora.Application.DTOs;
using Ilevora.Application.Interfaces;
using Ilevora.Application.Services;
using Ilevora.Domain.Crops;
using Ilevora.Domain.Exceptions;
using MediatR;

namespace Ilevora.Application.Queries.Crop;

public record GetCropsQuery(string FarmId) : IRequest<List<CropDto>>;

public class GetCropsQueryHandler : IRequestHandler<GetCropsQuery, List<CropDto>>
{
    private readonly ICropRepository _cropRepo;
    private readonly IFarmRepository _farmRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public GetCropsQueryHandler(ICropRepository cropRepo, IFarmRepository farmRepo, ICurrentUserAccessor currentUser)
    {
        _cropRepo = cropRepo;
        _farmRepo = farmRepo;
        _currentUser = currentUser;
    }

    /// <summary>The built-in crops, then the farm's own by name.</summary>
    public async Task<List<CropDto>> Handle(GetCropsQuery query, CancellationToken ct)
    {
        var farm = await _farmRepo.GetByIdAsync(query.FarmId, ct)
            ?? throw new NotFoundException("Farm", query.FarmId);
        if (!string.IsNullOrEmpty(_currentUser.CompanyId) && farm.CompanyId != _currentUser.CompanyId)
            throw new NotFoundException("Farm", query.FarmId);

        var list = BuiltInCrop.All.Select(b => new CropDto(b.Id, b.Name, true, CropResolver.WireOrdinal(b.Id))).ToList();
        var custom = await _cropRepo.GetByFarmIdAsync(farm.Id, ct);
        list.AddRange(custom.OrderBy(c => c.Name).Select(c => new CropDto(c.Id, c.Name, false, CropResolver.WireOrdinal(c.Id))));
        return list;
    }
}
