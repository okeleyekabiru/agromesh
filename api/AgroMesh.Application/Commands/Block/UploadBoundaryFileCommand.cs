using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Services;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.Utilities;
using MediatR;

namespace AgroMesh.Application.Commands.Block;

public record UploadBoundaryFileCommand(string BlockId, UploadBoundaryFileRequest Request) : IRequest<BoundarySubmissionResultDto>;

public class UploadBoundaryFileCommandHandler : IRequestHandler<UploadBoundaryFileCommand, BoundarySubmissionResultDto>
{
    private readonly IBlockRepository _blockRepo;
    private readonly IFarmRepository _farmRepo;
    private readonly IBoundarySubmissionRepository _submissionRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public UploadBoundaryFileCommandHandler(
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

    public async Task<BoundarySubmissionResultDto> Handle(UploadBoundaryFileCommand command, CancellationToken ct)
    {
        var block = await _blockRepo.GetByIdAsync(command.BlockId, ct)
            ?? throw new NotFoundException("Block", command.BlockId);

        await EnsureOwnedByCurrentCompanyAsync(block, ct);

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

        block.ApplyBoundary(geoJson, hectares, centroid.Lat, centroid.Lng, BoundarySource.SurveyUpload);
        await _blockRepo.UpdateAsync(block, ct);

        var submission = BoundarySubmission.Create(
            command.BlockId, command.Request.OperationId, BoundarySubmissionType.FileUpload,
            null, geoJson, hectares, centroid.Lat, centroid.Lng);
        await _submissionRepo.AddAsync(submission, ct);

        return SubmitBoundaryPointsCommandHandler.MapToDto(submission);
    }

    private async Task EnsureOwnedByCurrentCompanyAsync(Domain.Entities.Block block, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_currentUser.CompanyId))
            return;

        var farm = await _farmRepo.GetByIdAsync(block.FarmId, ct);
        if (farm is null || farm.CompanyId != _currentUser.CompanyId)
            throw new NotFoundException("Block", block.Id);
    }
}
