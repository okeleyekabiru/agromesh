using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.Utilities;
using AgroMesh.Domain.ValueObjects;
using MediatR;

namespace AgroMesh.Application.Commands.Section;

public record CreateSectionCommand(string FarmId, CreateSectionRequest Request) : IRequest<SectionDto>;

public class CreateSectionCommandHandler : IRequestHandler<CreateSectionCommand, SectionDto>
{
    private readonly ISectionRepository _sectionRepo;
    private readonly IFarmRepository _farmRepo;

    public CreateSectionCommandHandler(ISectionRepository sectionRepo, IFarmRepository farmRepo)
    {
        _sectionRepo = sectionRepo;
        _farmRepo = farmRepo;
    }

    public async Task<SectionDto> Handle(CreateSectionCommand command, CancellationToken ct)
    {
        var farm = await _farmRepo.GetByIdAsync(command.FarmId, ct)
            ?? throw new DomainException($"Farm '{command.FarmId}' not found.");

        var req = command.Request;
        var totalHectares = AreaConverter.ToHectares(req.AreaValue, farm.PreferredAreaUnit);

        var crops = req.Crops.Select(c => new SectionCrop(
            c.CropType,
            Math.Round(totalHectares * (c.Percentage / 100.0), 4),
            (int)c.Percentage)).ToList();

        var section = Domain.Entities.Section.Create(command.FarmId, req.Name, crops, totalHectares, req.BoundaryGeoJson);
        if (req.CentroidLat.HasValue && req.CentroidLng.HasValue && req.BoundaryGeoJson != null)
            section.SetBoundary(req.BoundaryGeoJson, req.CentroidLat, req.CentroidLng);

        await _sectionRepo.AddAsync(section, ct);

        if (farm.CentroidLat == null && req.CentroidLat.HasValue)
        {
            farm.SetCoordinates(req.CentroidLat.Value, req.CentroidLng!.Value);
            await _farmRepo.UpdateAsync(farm, ct);
        }

        return MapToDto(section, farm.PreferredAreaUnit);
    }

    public static SectionDto MapToDto(Domain.Entities.Section section, Domain.Enums.AreaUnit unit) => new(
        section.Id, section.FarmId, section.Name,
        section.Crops.Select(c => new SectionCropDto(c.CropType, AreaDto.From(c.Hectares, unit), c.Percentage)).ToList(),
        AreaDto.From(section.Hectares, unit),
        section.Status, section.LastActivityDate, section.BoundaryGeoJson,
        section.CentroidLat, section.CentroidLng,
        section.Blocks.Select(b => new BlockDto(b.Id, b.SectionId, b.Name, AreaDto.From(b.Hectares, unit), b.TreeCount)).ToList(),
        null, null);
}
