namespace AgroMesh.Application.DTOs;

public record RegisterRequest(
    string FullName,
    string Email,
    string Password,
    string FarmName,
    string? Location,
    double? Latitude,
    double? Longitude);

public record LoginRequest(
    string Email,
    string Password);

public record AuthUserDto(
    string Id,
    string Email,
    string FullName,
    string? FarmId);

public record AuthResponse(
    string Token,
    AuthUserDto User,
    string? FarmId);
