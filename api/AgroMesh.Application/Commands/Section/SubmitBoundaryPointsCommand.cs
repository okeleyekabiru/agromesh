using System.Text.Json;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.Utilities;
using AgroMesh.Domain.ValueObjects;
using MediatR;

namespace AgroMesh.Application.Commands.Section;

public record SubmitBoundaryPointsCommand(string SectionId, SubmitBoundaryPointsRequest Request) : IRequest<BoundarySubmissionResultDto>;

public class SubmitBoundaryPointsCommandHandler : IRequestHandler<SubmitBoundaryPointsCommand, BoundarySubmissionResultDto>
{
    private readonly ISectionRepository _sectionRepo;
    private readonly IFarmRepository _farmRepo;
    private readonly IBoundarySubmissionRepository _submissionRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public SubmitBoundaryPointsCommandHandler(
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

    public async Task<BoundarySubmissionResultDto> Handle(SubmitBoundaryPointsCommand command, CancellationToken ct)
    {
        var section = await _sectionRepo.GetByIdAsync(command.SectionId, ct)
            ?? throw new NotFoundException("Section", command.SectionId);

        await EnsureOwnedByCurrentCompanyAsync(section, ct);

        // "Finish boundary" is the one boundary-defining event: the client buffers the whole
        // capture session locally and sends it once under a client-generated OperationId. On a
        // retry of the same OperationId, return the previously computed result rather than a
        // silent no-op, so a client that timed out gets a coherent answer either way.
        var existing = await _submissionRepo.GetByOperationIdAsync(command.Request.OperationId, ct);
        if (existing is not null)
            return MapToDto(existing);

        var points = command.Request.Points.Select(p => new GeoPoint(p.Lat, p.Lng)).ToList();
        PolygonValidator.Validate(points, command.Request.ConfirmImplausibleDistance);

        var hectares = GeoPolygonAreaCalculator.CalculateHectares(points);
        var centroid = GeoPolygonAreaCalculator.CalculateCentroid(points);
        var geoJson = GeoJsonPolygonBuilder.Build(points);

        section.ApplyBoundary(geoJson, hectares, centroid.Lat, centroid.Lng, BoundarySource.GpsCapture);
        await _sectionRepo.UpdateAsync(section, ct);

        var submission = BoundarySubmission.Create(
            command.SectionId, command.Request.OperationId, BoundarySubmissionType.GpsPoints,
            JsonSerializer.Serialize(points), geoJson, hectares, centroid.Lat, centroid.Lng);
        await _submissionRepo.AddAsync(submission, ct);

        return MapToDto(submission);
    }

    private async Task EnsureOwnedByCurrentCompanyAsync(Domain.Entities.Section section, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_currentUser.CompanyId))
            return;

        var farm = await _farmRepo.GetByIdAsync(section.FarmId, ct);
        if (farm is null || farm.CompanyId != _currentUser.CompanyId)
            throw new NotFoundException("Section", section.Id);
    }

    public static BoundarySubmissionResultDto MapToDto(BoundarySubmission s) => new(
        s.SectionId, s.OperationId, s.SubmissionType, s.ComputedGeoJson, s.ComputedHectares,
        s.ComputedCentroidLat, s.ComputedCentroidLng);
}
