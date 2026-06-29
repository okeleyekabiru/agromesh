using AgroMesh.Domain.Enums;

namespace AgroMesh.Application.DTOs;

public record WorkerLogDto(string WorkerId, string WorkerName, WorkerType WorkerType, double HoursWorked, decimal DailyRate);

public record InputUsedDto(string Item, double Quantity, string Unit);

public record ActivityLogDto(
    string Id,
    string FarmId,
    string SectionId,
    string BlockId,
    ActivityType ActivityType,
    DateTime Date,
    string? Notes,
    string SupervisorId,
    decimal LabourCost,
    decimal InputCost,
    bool CreatedOffline,
    DateTime? SyncedAt,
    string? WorkedAreaGeoJson,
    AreaDto? WorkedArea,
    string? ChemicalProductId,
    string? ChemicalProductName,
    List<WorkerLogDto> Workers,
    List<InputUsedDto> Inputs);

public record LogActivityResponse(ActivityLogDto Activity, List<string> LowStockWarnings);

public record WorkerLogInput(string WorkerId, double HoursWorked);

public record InputUsedRequest(string Item, double Quantity, string Unit);

public record LogActivityRequest(
    string SectionId,
    string BlockId,
    ActivityType ActivityType,
    DateTime Date,
    List<WorkerLogInput> Workers,
    List<InputUsedRequest> Inputs,
    string SupervisorId,
    string? Notes,
    bool CreatedOffline,
    string? WorkedAreaGeoJson,
    double? WorkedAreaValue,
    string? ChemicalProductId);

public record PagedResult<T>(List<T> Items, int Total, int Page, int PageSize);
