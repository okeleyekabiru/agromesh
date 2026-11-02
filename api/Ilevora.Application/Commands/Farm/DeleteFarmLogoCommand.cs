using Ilevora.Application.Interfaces;
using Ilevora.Application.Services;
using MediatR;

namespace Ilevora.Application.Commands.Farm;

public record DeleteFarmLogoCommand(string FarmId) : IRequest;

public class DeleteFarmLogoCommandHandler : IRequestHandler<DeleteFarmLogoCommand>
{
    private readonly IFarmRepository _farmRepo;
    private readonly IStoredFileRepository _files;
    private readonly ICurrentUserAccessor _currentUser;

    public DeleteFarmLogoCommandHandler(IFarmRepository farmRepo, IStoredFileRepository files, ICurrentUserAccessor currentUser)
    {
        _farmRepo = farmRepo;
        _files = files;
        _currentUser = currentUser;
    }

    /// <summary>Idempotent: removing a logo that is not there succeeds.</summary>
    public async Task Handle(DeleteFarmLogoCommand command, CancellationToken ct)
    {
        var farm = await FarmAccess.GetFarmAsync(_farmRepo, _currentUser, command.FarmId, ct);
        FarmAccess.RequireManager(_currentUser, "change the farm logo");

        var fileId = farm.LogoFileId;
        if (fileId is null) return;

        farm.ClearLogo();
        await _farmRepo.UpdateAsync(farm, ct);
        await _files.DeleteAsync(fileId, ct);
    }
}
