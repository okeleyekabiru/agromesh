using Ilevora.Application.DTOs;
using Ilevora.Application.Interfaces;
using Ilevora.Application.Services;
using Ilevora.Domain.Exceptions;
using Ilevora.Domain.Utilities;
using Ilevora.Domain.ValueObjects;
using MediatR;

namespace Ilevora.Application.Commands.Block;

public record CreateBlockCommand(string FarmId, CreateBlockRequest Request) : IRequest<BlockDto>;

public class CreateBlockCommandHandler : IRequestHandler<CreateBlockCommand, BlockDto>
{
    private readonly IBlockRepository _blockRepo;
    private readonly IFarmRepository _farmRepo;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ICropRepository _cropRepo;

    public CreateBlockCommandHandler(IBlockRepository blockRepo, IFarmRepository farmRepo, ICurrentUserAccessor currentUser, ICropRepository cropRepo)
    {
        _blockRepo = blockRepo;
        _farmRepo = farmRepo;
        _currentUser = currentUser;
        _cropRepo = cropRepo;
    }

    public async Task<BlockDto> Handle(CreateBlockCommand command, CancellationToken ct)
    {
        var farm = await _farmRepo.GetByIdAsync(command.FarmId, ct)
            ?? throw new NotFoundException("Farm", command.FarmId);

        if (!string.IsNullOrEmpty(_currentUser.CompanyId) && farm.CompanyId != _currentUser.CompanyId)
            throw new NotFoundException("Farm", command.FarmId);

        var req = command.Request;
        // Guards against typos like an extra run of digits, which would wreck every per-area figure.
        if (!double.IsFinite(req.AreaValue) || req.AreaValue < 0 || req.AreaValue > 100_000)
            throw new DomainException("Area must be between 0 and 100,000.");
        var totalHectares = AreaConverter.ToHectares(req.AreaValue, farm.PreferredAreaUnit);

        var crops = await CropResolver.ResolveAsync(_cropRepo, farm.Id, req.Crops, totalHectares, ct);

        var block = Domain.Entities.Block.Create(command.FarmId, req.Name, crops, totalHectares, treeCount: req.TreeCount, plantingYear: req.PlantingYear);
        if (totalHectares > 0)
            block.SetManualHectares(totalHectares);

        await _blockRepo.AddAsync(block, ct);

        return MapToDto(block, farm.PreferredAreaUnit);
    }

    public static BlockDto MapToDto(Domain.Entities.Block block, Domain.Enums.AreaUnit unit) => new(
        block.Id, block.FarmId, block.Name,
        block.Crops.Select(c => new BlockCropDto(CropResolver.WireOrdinal(c.CropId), c.CropId, c.CropName, AreaDto.From(c.Hectares, unit), c.Percentage)).ToList(),
        AreaDto.From(block.Hectares, unit),
        block.Status, block.LastActivityDate, block.BoundaryGeoJson, block.BoundarySource,
        block.CentroidLat, block.CentroidLng, block.TreeCount,
        null, null, block.PlantingYear);
}
