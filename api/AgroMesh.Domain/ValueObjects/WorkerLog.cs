using AgroMesh.Domain.Enums;

namespace AgroMesh.Domain.ValueObjects;

public record WorkerLog(
    string WorkerId,
    string WorkerName,
    WorkerType WorkerType,
    double HoursWorked,
    decimal PayRate,
    PayFrequency PayFrequency
);
