using AgroMesh.Domain.Enums;

namespace AgroMesh.Application.DTOs;

public record RegisterRequest(
    string FullName,
    string Email,
    string Password,
    string FarmName);

public record LoginRequest(
    string Email,
    string Password);

// Role is a string ("Admin"/"Member") rather than the raw UserRole enum: this codebase has no
// JsonStringEnumConverter configured (enums serialize as raw ints on the wire otherwise — see
// PAY_FREQUENCY_LABELS' doc comment in web/src/types/index.ts), and this DTO is hand-constructed
// by its callers rather than auto-mapped, so using .ToString() here sidesteps that ambiguity
// entirely instead of repeating it.
public record AuthUserDto(
    string Id,
    string Email,
    string FullName,
    string? CompanyId,
    string Role);

public record FarmSummaryDto(
    string Id,
    string Name,
    FarmSetupStatus SetupStatus);

public record AuthResponse(
    string Token,
    AuthUserDto User,
    string? CompanyId,
    List<FarmSummaryDto> Farms);
