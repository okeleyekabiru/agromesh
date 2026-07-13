using AgroMesh.Domain.Enums;

namespace AgroMesh.Application.Interfaces;

public interface ICurrentUserAccessor
{
    string? UserId { get; }
    string? CompanyId { get; }

    // Fails closed: implementations must default to UserRole.Member when the caller's role can't
    // be determined (e.g. a JWT issued before the "role" claim existed) — never silently Admin.
    UserRole Role { get; }

    bool IsAdmin => Role == UserRole.Admin;
}
