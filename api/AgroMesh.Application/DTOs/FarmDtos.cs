using AgroMesh.Domain.Enums;

namespace AgroMesh.Application.DTOs;

public record FarmDto(
    string Id,
    string Name,
    string Location,
    AreaDto Area,
    string OwnerId,
    string CompanyId,
    string CountryCode,
    string? StateId,
    string? LgaFreetext,
    FarmSetupStatus SetupStatus,
    AreaUnit PreferredAreaUnit,
    double? CentroidLat,
    double? CentroidLng,
    int SectionCount,
    int WorkerCount);

public record CreateFarmRequest(string Name, string StateId, string? LgaFreetext);

public record SetFarmLocationRequest(string StateId, string? LgaFreetext);

public record UpdateFarmProfileRequest(string Name, double AreaValue, AreaUnit AreaUnit);
