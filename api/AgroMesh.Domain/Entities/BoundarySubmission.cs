using AgroMesh.Domain.Enums;

namespace AgroMesh.Domain.Entities;

public class BoundarySubmission : BaseEntity
{
    public string SectionId { get; private set; } = string.Empty;
    public string OperationId { get; private set; } = string.Empty;
    public BoundarySubmissionType SubmissionType { get; private set; }
    public string? RawPointsJson { get; private set; }
    public string ComputedGeoJson { get; private set; } = string.Empty;
    public double ComputedHectares { get; private set; }
    public double? ComputedCentroidLat { get; private set; }
    public double? ComputedCentroidLng { get; private set; }

    private BoundarySubmission() { }

    public static BoundarySubmission Create(
        string sectionId,
        string operationId,
        BoundarySubmissionType submissionType,
        string? rawPointsJson,
        string computedGeoJson,
        double computedHectares,
        double? computedCentroidLat,
        double? computedCentroidLng) => new()
    {
        SectionId = sectionId,
        OperationId = operationId,
        SubmissionType = submissionType,
        RawPointsJson = rawPointsJson,
        ComputedGeoJson = computedGeoJson,
        ComputedHectares = computedHectares,
        ComputedCentroidLat = computedCentroidLat,
        ComputedCentroidLng = computedCentroidLng
    };
}
