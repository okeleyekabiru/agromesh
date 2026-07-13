using System.IdentityModel.Tokens.Jwt;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace AgroMesh.Infrastructure.Services.Auth;

public class CurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserAccessor(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

    public string? UserId => NullIfEmpty(_httpContextAccessor.HttpContext?.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

    public string? CompanyId => NullIfEmpty(_httpContextAccessor.HttpContext?.User?.FindFirst("companyId")?.Value);

    // Fail closed: a token minted before the "role" claim existed (or any unparseable value) is
    // treated as Member, never silently upgraded to Admin.
    public UserRole Role =>
        Enum.TryParse<UserRole>(_httpContextAccessor.HttpContext?.User?.FindFirst("role")?.Value, out var role)
            ? role
            : UserRole.Member;

    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;
}
