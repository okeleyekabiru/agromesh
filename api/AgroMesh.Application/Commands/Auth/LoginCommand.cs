using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Commands.Auth;

public record LoginCommand(LoginRequest Request) : IRequest<AuthResponse>;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IUserRepository _userRepo;
    private readonly IFarmRepository _farmRepo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;

    public LoginCommandHandler(IUserRepository userRepo, IFarmRepository farmRepo, IPasswordHasher passwordHasher, IJwtService jwtService)
    {
        _userRepo = userRepo;
        _farmRepo = farmRepo;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
    }

    public async Task<AuthResponse> Handle(LoginCommand command, CancellationToken ct)
    {
        var req = command.Request;

        var user = await _userRepo.GetByEmailAsync(req.Email, ct)
            ?? throw new DomainException("Invalid email or password.");

        if (!_passwordHasher.Verify(req.Password, user.PasswordHash))
            throw new DomainException("Invalid email or password.");

        var farms = string.IsNullOrEmpty(user.CompanyId)
            ? []
            : await _farmRepo.GetByCompanyIdAsync(user.CompanyId, ct);

        var token = _jwtService.GenerateToken(user);
        return new AuthResponse(
            token,
            new AuthUserDto(user.Id, user.Email, user.FullName, user.CompanyId, user.Role.ToString()),
            user.CompanyId,
            farms.Select(f => new FarmSummaryDto(f.Id, f.Name, f.SetupStatus)).ToList());
    }
}
