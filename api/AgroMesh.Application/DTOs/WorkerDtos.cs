using AgroMesh.Domain.Enums;

namespace AgroMesh.Application.DTOs;

public record WorkerRoleDto(string Id, string FarmId, string Title, WorkerType DefaultType, decimal DailyRate, int WorkerCount);

public record CreateWorkerRoleRequest(string Title, WorkerType DefaultType, decimal DailyRate);

public record UpdateWorkerRoleRateRequest(decimal DailyRate);

public record WorkerDto(
    string Id,
    string FarmId,
    string Name,
    string Initials,
    string WorkerRoleId,
    string RoleTitle,
    decimal DailyRate,
    WorkerType Type,
    bool Active,
    int DaysLoggedThisMonth);

public record AddWorkerRequest(string Name, string Initials, string WorkerRoleId, WorkerType Type);
