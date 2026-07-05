using AgroMesh.Domain.Enums;

namespace AgroMesh.Application.DTOs;

public record SectionCropDto(CropType CropType, AreaDto Area, int Percentage);

public record SectionDto(
    string Id,
    string FarmId,
    string Name,
    List<SectionCropDto> Crops,
    AreaDto Area,
    SectionStatus Status,
    DateTime? LastActivityDate,
    string? BoundaryGeoJson,
    double? CentroidLat,
    double? CentroidLng,
    List<BlockDto> Blocks,
    UpcomingTaskDto? NextWeedingPrediction,
    UpcomingTaskDto? NextSprayingPrediction);

public record BlockDto(string Id, string SectionId, string Name, AreaDto Area, int? TreeCount);

public record SectionCropInput(CropType CropType, double Percentage);

// Boundary capture (GPS corner capture or GeoJSON/KML upload) happens as a separate step
// after creation, via the dedicated boundary endpoints — not bundled into section creation.
public record CreateSectionRequest(string Name, List<SectionCropInput> Crops, double AreaValue);

public record CreateBlockRequest(string Name, double AreaValue, int? TreeCount);
