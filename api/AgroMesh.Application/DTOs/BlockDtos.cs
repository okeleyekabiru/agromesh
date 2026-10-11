using AgroMesh.Domain.Enums;

namespace AgroMesh.Application.DTOs;

public record BlockCropDto(CropType CropType, AreaDto Area, int Percentage);

public record BlockDto(
    string Id,
    string FarmId,
    string Name,
    List<BlockCropDto> Crops,
    AreaDto Area,
    BlockStatus Status,
    DateTime? LastActivityDate,
    string? BoundaryGeoJson,
    BoundarySource? BoundarySource,
    double? CentroidLat,
    double? CentroidLng,
    int? TreeCount,
    UpcomingTaskDto? NextWeedingPrediction,
    UpcomingTaskDto? NextSprayingPrediction,
    int? PlantingYear = null,
    UpcomingTaskDto? NextFertilizingPrediction = null);

public record BlockCropInput(CropType CropType, double Percentage);

// Boundary capture (GPS corner capture or GeoJSON/KML upload) happens as a separate step
// after creation, via the dedicated boundary endpoints — not bundled into block creation.
public record CreateBlockRequest(string Name, List<BlockCropInput> Crops, double AreaValue, int? TreeCount = null, int? PlantingYear = null);

public record UpdateBlockRequest(string Name, List<BlockCropInput> Crops, double AreaValue, int? TreeCount = null, int? PlantingYear = null);
