using AgroMesh.Domain.Enums;

namespace AgroMesh.Application.DTOs;

public record GeoPointInput(double Lat, double Lng);

public record SubmitBoundaryPointsRequest(string OperationId, List<GeoPointInput> Points, bool ConfirmImplausibleGeometry = false);

public record UploadBoundaryFileRequest(string OperationId, string FileName, string Content, bool ConfirmImplausibleGeometry = false);

public record BoundarySubmissionResultDto(
    string BlockId,
    string OperationId,
    BoundarySubmissionType SubmissionType,
    string ComputedGeoJson,
    double ComputedHectares,
    double? ComputedCentroidLat,
    double? ComputedCentroidLng);
