namespace AgroMesh.Application.DTOs;

/// <summary>What the farm logo currently is. UpdatedAt changes on every upload, so a client can use it to refresh its cache.</summary>
public record FarmLogoDto(bool HasLogo, string? ContentType, long? Size, DateTime? UpdatedAt);

/// <summary>The logo bytes with what a response needs to send them.</summary>
public record FarmLogoFile(byte[] Content, string ContentType, string FileName, DateTime? UpdatedAt);

/// <summary>How the farm presents itself to customers. Printed on invoices.</summary>
public record FarmBusinessProfileDto(
    string? Address,
    string? Phone,
    string? Email,
    string? Website,
    string? BankAccountName,
    string? BankAccountNumber,
    string? BankName);

/// <summary>A full replacement: a field left out (null or blank) is cleared.</summary>
public record UpdateFarmBusinessProfileRequest(
    string? Address,
    string? Phone,
    string? Email,
    string? Website,
    string? BankAccountName,
    string? BankAccountNumber,
    string? BankName);
