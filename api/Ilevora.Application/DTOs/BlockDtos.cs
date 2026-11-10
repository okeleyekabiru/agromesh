using Ilevora.Domain.Enums;


namespace Ilevora.Application.DTOs;

// CropType keeps the old enum ordinal on the wire for older clients (OilPalm 0, Maize 1, Mixed 2, Cocoa 3, Cassava 4); crops added since send 2.
// New clients use CropId + CropName.
public record BlockCropDto(int CropType, string CropId, string CropName, AreaDto Area, int Percentage);

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

// A crop is named by CropId (built-in code or custom crop id); CropType (old enum ordinal) is accepted when CropId is absent.
public record BlockCropInput(double Percentage, string? CropId = null, int? CropType = null);

public record CropDto(string Id, string Name, bool IsBuiltIn, int CropType);

public record CreateCropRequest(string Name);

// Boundary capture (GPS corner capture or GeoJSON/KML upload) happens as a separate step
// after creation, via the dedicated boundary endpoints — not bundled into block creation.
public record CreateBlockRequest(string Name, List<BlockCropInput> Crops, double AreaValue, int? TreeCount = null, int? PlantingYear = null);

public record UpdateBlockRequest(string Name, List<BlockCropInput> Crops, double AreaValue, int? TreeCount = null, int? PlantingYear = null);
