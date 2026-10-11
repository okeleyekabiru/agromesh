using System.Text.Json;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.Utilities;
using AgroMesh.Domain.ValueObjects;
using MediatR;

namespace AgroMesh.Application.Commands.Block;

public record SubmitBoundaryPointsCommand(string BlockId, SubmitBoundaryPointsRequest Request) : IRequest<BoundarySubmissionResultDto>;

public class SubmitBoundaryPointsCommandHandler : IRequestHandler<SubmitBoundaryPointsCommand, BoundarySubmissionResultDto>
{
    private readonly IBlockRepository _blockRepo;
    private readonly IFarmRepository _farmRepo;
    private readonly IBoundarySubmissionRepository _submissionRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public SubmitBoundaryPointsCommandHandler(
        IBlockRepository blockRepo,
        IFarmRepository farmRepo,
        IBoundarySubmissionRepository submissionRepo,
        ICurrentUserAccessor currentUser)
    {
        _blockRepo = blockRepo;
        _farmRepo = farmRepo;
        _submissionRepo = submissionRepo;
        _currentUser = currentUser;
    }

    public async Task<BoundarySubmissionResultDto> Handle(SubmitBoundaryPointsCommand command, CancellationToken ct)
    {
        var block = await _blockRepo.GetByIdAsync(command.BlockId, ct)
            ?? throw new NotFoundException("Block", command.BlockId);

        await EnsureOwnedByCurrentCompanyAsync(block, ct);

        // "Finish boundary" is the one boundary-defining event: the client buffers the whole
        // capture session locally and sends it once under a client-generated OperationId. On a
        // retry of the same OperationId, return the previously computed result rather than a
        // silent no-op, so a client that timed out gets a coherent answer either way.
        var existing = await _submissionRepo.GetByOperationIdAsync(command.Request.OperationId, ct);
        if (existing is not null)
            return MapToDto(existing);

        var points = command.Request.Points.Select(p => new GeoPoint(p.Lat, p.Lng)).ToList();
        PolygonValidator.Validate(points, command.Request.ConfirmImplausibleGeometry);

        var hectares = GeoPolygonAreaCalculator.CalculateHectares(points);
        PolygonValidator.ValidateArea(points, hectares, command.Request.ConfirmImplausibleGeometry);

        var centroid = GeoPolygonAreaCalculator.CalculateCentroid(points);
        var geoJson = GeoJsonPolygonBuilder.Build(points);

        block.ApplyBoundary(geoJson, hectares, centroid.Lat, centroid.Lng, BoundarySource.GpsCapture);
        await _blockRepo.UpdateAsync(block, ct);

        var submission = BoundarySubmission.Create(
            command.BlockId, command.Request.OperationId, BoundarySubmissionType.GpsPoints,
            JsonSerializer.Serialize(points), geoJson, hectares, centroid.Lat, centroid.Lng);
        await _submissionRepo.AddAsync(submission, ct);

        return MapToDto(submission);
    }

    private async Task EnsureOwnedByCurrentCompanyAsync(Domain.Entities.Block block, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_currentUser.CompanyId))
            return;

        var farm = await _farmRepo.GetByIdAsync(block.FarmId, ct);
        if (farm is null || farm.CompanyId != _currentUser.CompanyId)
            throw new NotFoundException("Block", block.Id);
    }

    public static BoundarySubmissionResultDto MapToDto(BoundarySubmission s) => new(
        s.BlockId, s.OperationId, s.SubmissionType, s.ComputedGeoJson, s.ComputedHectares,
        s.ComputedCentroidLat, s.ComputedCentroidLng);
}
