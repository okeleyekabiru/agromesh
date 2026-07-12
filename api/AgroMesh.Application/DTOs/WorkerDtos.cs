using AgroMesh.Domain.Enums;

namespace AgroMesh.Application.DTOs;

public record WorkerRoleDto(string Id, string FarmId, string Title, WorkerType DefaultType, decimal PayRate, PayFrequency PayFrequency, int WorkerCount);

public record CreateWorkerRoleRequest(string Title, WorkerType DefaultType, decimal PayRate, PayFrequency PayFrequency);

public record UpdateWorkerRoleRateRequest(decimal PayRate, PayFrequency PayFrequency);

public record WorkerDto(
    string Id,
    string FarmId,
    string Name,
    string Initials,
    string WorkerRoleId,
    string RoleTitle,
    decimal PayRate,
    PayFrequency PayFrequency,
    WorkerType Type,
    bool Active,
    int DaysLoggedThisMonth);

public record AddWorkerRequest(string Name, string Initials, string WorkerRoleId, WorkerType Type);
