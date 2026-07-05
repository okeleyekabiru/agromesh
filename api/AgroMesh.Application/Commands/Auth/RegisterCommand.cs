using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Commands.Auth;

public record RegisterCommand(RegisterRequest Request) : IRequest<AuthResponse>;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IUserRepository _userRepo;
    private readonly ICompanyRepository _companyRepo;
    private readonly IFarmRepository _farmRepo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;

    public RegisterCommandHandler(
        IUserRepository userRepo,
        ICompanyRepository companyRepo,
        IFarmRepository farmRepo,
        IPasswordHasher passwordHasher,
        IJwtService jwtService)
    {
        _userRepo = userRepo;
        _companyRepo = companyRepo;
        _farmRepo = farmRepo;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
    }

    public async Task<AuthResponse> Handle(RegisterCommand command, CancellationToken ct)
    {
        var req = command.Request;

        var existing = await _userRepo.GetByEmailAsync(req.Email, ct);
        if (existing is not null)
            throw new DomainException("An account with this email already exists.");

        var hash = _passwordHasher.Hash(req.Password);
        var user = User.Create(req.FullName, req.Email, hash);
        await _userRepo.AddAsync(user, ct);
        await _userRepo.SaveAsync(ct);

        var company = Domain.Entities.Company.Create(req.FarmName, user.Id, isDefaultCompany: true);
        await _companyRepo.AddAsync(company, ct);

        var farm = Domain.Entities.Farm.Create(req.FarmName, company.Id, user.Id);
        await _farmRepo.AddAsync(farm, ct);

        user.AssignCompany(company.Id);
        await _userRepo.SaveAsync(ct);

        var token = _jwtService.GenerateToken(user);
        var farmSummary = new FarmSummaryDto(farm.Id, farm.Name, farm.SetupStatus);
        return new AuthResponse(
            token,
            new AuthUserDto(user.Id, user.Email, user.FullName, user.CompanyId),
            user.CompanyId,
            [farmSummary]);
    }
}
