using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.Utilities;
using AgroMesh.Domain.ValueObjects;
using MediatR;

namespace AgroMesh.Application.Commands.Section;

public record UpdateSectionCommand(string SectionId, UpdateSectionRequest Request) : IRequest<SectionDto>;

public class UpdateSectionCommandHandler : IRequestHandler<UpdateSectionCommand, SectionDto>
{
    private readonly ISectionRepository _sectionRepo;
    private readonly IFarmRepository _farmRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public UpdateSectionCommandHandler(ISectionRepository sectionRepo, IFarmRepository farmRepo, ICurrentUserAccessor currentUser)
    {
        _sectionRepo = sectionRepo;
        _farmRepo = farmRepo;
        _currentUser = currentUser;
    }

    public async Task<SectionDto> Handle(UpdateSectionCommand command, CancellationToken ct)
    {
        var section = await _sectionRepo.GetByIdAsync(command.SectionId, ct)
            ?? throw new NotFoundException("Section", command.SectionId);

        var farm = await _farmRepo.GetByIdAsync(section.FarmId, ct)
            ?? throw new NotFoundException("Section", command.SectionId);

        if (!string.IsNullOrEmpty(_currentUser.CompanyId) && farm.CompanyId != _currentUser.CompanyId)
            throw new NotFoundException("Section", command.SectionId);

        var req = command.Request;
        var totalHectares = AreaConverter.ToHectares(req.AreaValue, farm.PreferredAreaUnit);

        var crops = req.Crops.Select(c => new SectionCrop(
            c.CropType,
            Math.Round(totalHectares * (c.Percentage / 100.0), 4),
            (int)c.Percentage)).ToList();

        section.Rename(req.Name);
        section.UpdateCrops(crops);

        // Only overwrite the area from the manual field when there's no real boundary yet —
        // once a boundary exists, its computed area is the source of truth and should only
        // change via a fresh boundary submission (POST .../boundary/points|upload).
        if (section.BoundaryGeoJson == null && totalHectares > 0)
            section.SetManualHectares(totalHectares);

        await _sectionRepo.UpdateAsync(section, ct);

        return CreateSectionCommandHandler.MapToDto(section, farm.PreferredAreaUnit);
    }
}
