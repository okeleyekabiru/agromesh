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

public record AuthUserDto(
    string Id,
    string Email,
    string FullName,
    string? CompanyId);

public record FarmSummaryDto(
    string Id,
    string Name,
    FarmSetupStatus SetupStatus);

public record AuthResponse(
    string Token,
    AuthUserDto User,
    string? CompanyId,
    List<FarmSummaryDto> Farms);
