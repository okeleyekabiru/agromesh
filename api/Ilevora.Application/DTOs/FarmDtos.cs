using Ilevora.Domain.Enums;

namespace Ilevora.Application.DTOs;

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
    int BlockCount,
    int WorkerCount,
    bool HasLogo = false,
    DateTime? LogoUpdatedAt = null);

public record CreateFarmRequest(string Name, string StateId, string? LgaFreetext);

public record SetFarmLocationRequest(string StateId, string? LgaFreetext);

public record UpdateFarmProfileRequest(string Name, double AreaValue, AreaUnit AreaUnit);
