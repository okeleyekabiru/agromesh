using Ilevora.Domain.Entities;

namespace Ilevora.Application.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user);
}
