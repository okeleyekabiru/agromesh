using System.Text.Json.Serialization;
using AgroMesh.Domain.Enums;

namespace AgroMesh.Application.DTOs;

public record WorkerLogDto(string WorkerId, string WorkerName, WorkerType WorkerType, double HoursWorked, decimal PayRate, PayFrequency PayFrequency);

public record InputUsedDto(string Item, double Quantity, string Unit);

public record ActivityLogDto(
    string Id,
    string FarmId,
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

// Inputs must be real stock items: the item's name and unit are taken from stock, not typed in.
public record InputUsedRequest(string InventoryItemId, double Quantity);

// ActivityType accepts either its name ("Weeding") or its ordinal: the web form sends names, the
// mobile app ordinals. Scoped to this property so every other DTO keeps the app-wide ordinal format.
public record LogActivityRequest(
    string BlockId,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] ActivityType ActivityType,
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
