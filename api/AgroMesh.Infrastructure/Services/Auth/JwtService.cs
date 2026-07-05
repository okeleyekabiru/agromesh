using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AgroMesh.Infrastructure.Services.Auth;

public class JwtService : IJwtService
{
    private readonly string _key;
    private readonly string _issuer;

    public JwtService(IConfiguration config)
    {
        _key    = config["Jwt:Key"]    ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        _issuer = config["Jwt:Issuer"] ?? "AgroMesh";
    }

    public string GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("fullName",                    user.FullName),
            new Claim("companyId",                    user.CompanyId ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
        };

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:             _issuer,
            audience:           _issuer,
            claims:             claims,
            expires:            DateTime.UtcNow.AddDays(30),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
