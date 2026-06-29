using AgroMesh.Domain.Enums;

namespace AgroMesh.Application.DTOs;

public record FarmDto(
    string Id,
    string Name,
    string Location,
    AreaDto Area,
    string OwnerId,
    AreaUnit PreferredAreaUnit,
    double? CentroidLat,
    double? CentroidLng,
    int SectionCount,
    int WorkerCount);

public record CreateFarmRequest(string Name, string Location, double AreaValue, AreaUnit AreaUnit, string OwnerId);
