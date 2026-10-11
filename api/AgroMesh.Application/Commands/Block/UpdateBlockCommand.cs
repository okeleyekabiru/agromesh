using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.Utilities;
using AgroMesh.Domain.ValueObjects;
using MediatR;

namespace AgroMesh.Application.Commands.Block;

public record UpdateBlockCommand(string BlockId, UpdateBlockRequest Request) : IRequest<BlockDto>;

public class UpdateBlockCommandHandler : IRequestHandler<UpdateBlockCommand, BlockDto>
{
    private readonly IBlockRepository _blockRepo;
    private readonly IFarmRepository _farmRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public UpdateBlockCommandHandler(IBlockRepository blockRepo, IFarmRepository farmRepo, ICurrentUserAccessor currentUser)
    {
        _blockRepo = blockRepo;
        _farmRepo = farmRepo;
        _currentUser = currentUser;
    }

    public async Task<BlockDto> Handle(UpdateBlockCommand command, CancellationToken ct)
    {
        var block = await _blockRepo.GetByIdAsync(command.BlockId, ct)
            ?? throw new NotFoundException("Block", command.BlockId);

        var farm = await _farmRepo.GetByIdAsync(block.FarmId, ct)
            ?? throw new NotFoundException("Block", command.BlockId);

        if (!string.IsNullOrEmpty(_currentUser.CompanyId) && farm.CompanyId != _currentUser.CompanyId)
            throw new NotFoundException("Block", command.BlockId);

        var req = command.Request;
        // Guards against typos like an extra run of digits, which would wreck every per-area figure.
        if (!double.IsFinite(req.AreaValue) || req.AreaValue < 0 || req.AreaValue > 100_000)
            throw new DomainException("Area must be between 0 and 100,000.");
        var totalHectares = AreaConverter.ToHectares(req.AreaValue, farm.PreferredAreaUnit);

        var crops = req.Crops.Select(c => new BlockCrop(
            c.CropType,
            Math.Round(totalHectares * (c.Percentage / 100.0), 4),
            (int)c.Percentage)).ToList();

        block.Rename(req.Name);
        block.UpdateCrops(crops);
        block.SetTreeCount(req.TreeCount);
        block.SetPlantingYear(req.PlantingYear);

        // Only overwrite the area from the manual field when there's no real boundary yet —
        // once a boundary exists, its computed area is the source of truth and should only
        // change via a fresh boundary submission (POST .../boundary/points|upload).
        // A boundary whose area is zero or absurd (bad GPS points) is not worth keeping: the area typed
        // here replaces it, otherwise the edit would silently do nothing.
        if (totalHectares > 0 && block.BoundaryGeoJson != null && !block.HasPlausibleArea)
            block.ClearBoundary();
        if (block.BoundaryGeoJson == null && totalHectares > 0)
            block.SetManualHectares(totalHectares);

        await _blockRepo.UpdateAsync(block, ct);

        return CreateBlockCommandHandler.MapToDto(block, farm.PreferredAreaUnit);
    }
}
