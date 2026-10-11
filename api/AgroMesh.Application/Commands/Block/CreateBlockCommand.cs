using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.Utilities;
using AgroMesh.Domain.ValueObjects;
using MediatR;

namespace AgroMesh.Application.Commands.Block;

public record CreateBlockCommand(string FarmId, CreateBlockRequest Request) : IRequest<BlockDto>;

public class CreateBlockCommandHandler : IRequestHandler<CreateBlockCommand, BlockDto>
{
    private readonly IBlockRepository _blockRepo;
    private readonly IFarmRepository _farmRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public CreateBlockCommandHandler(IBlockRepository blockRepo, IFarmRepository farmRepo, ICurrentUserAccessor currentUser)
    {
        _blockRepo = blockRepo;
        _farmRepo = farmRepo;
        _currentUser = currentUser;
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

        var crops = req.Crops.Select(c => new BlockCrop(
            c.CropType,
            Math.Round(totalHectares * (c.Percentage / 100.0), 4),
            (int)c.Percentage)).ToList();

        var block = Domain.Entities.Block.Create(command.FarmId, req.Name, crops, totalHectares, treeCount: req.TreeCount, plantingYear: req.PlantingYear);
        if (totalHectares > 0)
            block.SetManualHectares(totalHectares);

        await _blockRepo.AddAsync(block, ct);

        return MapToDto(block, farm.PreferredAreaUnit);
    }

    public static BlockDto MapToDto(Domain.Entities.Block block, Domain.Enums.AreaUnit unit) => new(
        block.Id, block.FarmId, block.Name,
        block.Crops.Select(c => new BlockCropDto(c.CropType, AreaDto.From(c.Hectares, unit), c.Percentage)).ToList(),
        AreaDto.From(block.Hectares, unit),
        block.Status, block.LastActivityDate, block.BoundaryGeoJson, block.BoundarySource,
        block.CentroidLat, block.CentroidLng, block.TreeCount,
        null, null, block.PlantingYear);
}
