using AgroMesh.Domain.Entities;

namespace AgroMesh.Application.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user);
}
