using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Services;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.Utilities;
using MediatR;

namespace AgroMesh.Application.Commands.Section;

public record UploadBoundaryFileCommand(string SectionId, UploadBoundaryFileRequest Request) : IRequest<BoundarySubmissionResultDto>;

public class UploadBoundaryFileCommandHandler : IRequestHandler<UploadBoundaryFileCommand, BoundarySubmissionResultDto>
{
    private readonly ISectionRepository _sectionRepo;
    private readonly IFarmRepository _farmRepo;
    private readonly IBoundarySubmissionRepository _submissionRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public UploadBoundaryFileCommandHandler(
        ISectionRepository sectionRepo,
        IFarmRepository farmRepo,
        IBoundarySubmissionRepository submissionRepo,
        ICurrentUserAccessor currentUser)
    {
        _sectionRepo = sectionRepo;
        _farmRepo = farmRepo;
        _submissionRepo = submissionRepo;
        _currentUser = currentUser;
    }

    public async Task<BoundarySubmissionResultDto> Handle(UploadBoundaryFileCommand command, CancellationToken ct)
    {
        var section = await _sectionRepo.GetByIdAsync(command.SectionId, ct)
            ?? throw new NotFoundException("Section", command.SectionId);

        await EnsureOwnedByCurrentCompanyAsync(section, ct);

        var existing = await _submissionRepo.GetByOperationIdAsync(command.Request.OperationId, ct);
        if (existing is not null)
            return SubmitBoundaryPointsCommandHandler.MapToDto(existing);

        var isKml = command.Request.FileName.EndsWith(".kml", StringComparison.OrdinalIgnoreCase);
        var points = isKml
            ? BoundaryFileParser.ParseKml(command.Request.Content)
            : BoundaryFileParser.ParseGeoJson(command.Request.Content);

        PolygonValidator.Validate(points, command.Request.ConfirmImplausibleGeometry);

        var hectares = GeoPolygonAreaCalculator.CalculateHectares(points);
        PolygonValidator.ValidateArea(points, hectares, command.Request.ConfirmImplausibleGeometry);

        var centroid = GeoPolygonAreaCalculator.CalculateCentroid(points);
        var geoJson = GeoJsonPolygonBuilder.Build(points);

        section.ApplyBoundary(geoJson, hectares, centroid.Lat, centroid.Lng, BoundarySource.SurveyUpload);
        await _sectionRepo.UpdateAsync(section, ct);

        var submission = BoundarySubmission.Create(
            command.SectionId, command.Request.OperationId, BoundarySubmissionType.FileUpload,
            null, geoJson, hectares, centroid.Lat, centroid.Lng);
        await _submissionRepo.AddAsync(submission, ct);

        return SubmitBoundaryPointsCommandHandler.MapToDto(submission);
    }

    private async Task EnsureOwnedByCurrentCompanyAsync(Domain.Entities.Section section, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_currentUser.CompanyId))
            return;

        var farm = await _farmRepo.GetByIdAsync(section.FarmId, ct);
        if (farm is null || farm.CompanyId != _currentUser.CompanyId)
            throw new NotFoundException("Section", section.Id);
    }
}
