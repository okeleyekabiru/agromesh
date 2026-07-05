using System.IdentityModel.Tokens.Jwt;
using AgroMesh.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace AgroMesh.Infrastructure.Services.Auth;

public class CurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserAccessor(IHttpContextAccessor httpContextAccessor) => _httpContextAccessor = httpContextAccessor;

    public string? UserId => NullIfEmpty(_httpContextAccessor.HttpContext?.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

    public string? CompanyId => NullIfEmpty(_httpContextAccessor.HttpContext?.User?.FindFirst("companyId")?.Value);

    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;
}
