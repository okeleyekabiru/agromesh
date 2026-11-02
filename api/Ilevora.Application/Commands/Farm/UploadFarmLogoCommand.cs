using Ilevora.Application.DTOs;
using Ilevora.Application.Interfaces;
using Ilevora.Application.Services;
using Ilevora.Domain.Entities;
using Ilevora.Domain.Exceptions;
using MediatR;

namespace Ilevora.Application.Commands.Farm;

/// <summary>Sets or replaces the farm logo. The same call does both: the old file is removed once the new one is saved.</summary>
public record UploadFarmLogoCommand(string FarmId, byte[] Content, string? FileName, string? ContentType) : IRequest<FarmLogoDto>;

public class UploadFarmLogoCommandHandler : IRequestHandler<UploadFarmLogoCommand, FarmLogoDto>
{
    public const int MaxPixels = 4096;

    private readonly IFarmRepository _farmRepo;
    private readonly IStoredFileRepository _files;
    private readonly IImageInspector _images;
    private readonly ICurrentUserAccessor _currentUser;

    public UploadFarmLogoCommandHandler(IFarmRepository farmRepo, IStoredFileRepository files, IImageInspector images, ICurrentUserAccessor currentUser)
    {
        _farmRepo = farmRepo;
        _files = files;
        _images = images;
        _currentUser = currentUser;
    }

    public async Task<FarmLogoDto> Handle(UploadFarmLogoCommand command, CancellationToken ct)
    {
        var farm = await FarmAccess.GetFarmAsync(_farmRepo, _currentUser, command.FarmId, ct);
        FarmAccess.RequireManager(_currentUser, "change the farm logo");

        var upload = UploadedFileValidator.Validate(command.Content, command.ContentType, UploadRules.Logo);

        var image = _images.Inspect(command.Content)
            ?? throw new UnsupportedFileTypeException("The file is not a readable image.");
        if (image.Width > MaxPixels || image.Height > MaxPixels)
            throw new DomainException($"The logo is too large in pixels. The limit is {MaxPixels} x {MaxPixels}.");

        var fileName = UploadedFileValidator.SafeFileName(command.FileName, "logo" + upload.Extension);
        var stored = await _files.AddAsync(StoredFile.Create(farm.Id, fileName, upload.ContentType, command.Content), ct);

        var previous = farm.LogoFileId;
        farm.SetLogo(stored.Id);
        try
        {
            await _farmRepo.UpdateAsync(farm, ct);
        }
        catch
        {
            // Do not leave an orphaned file behind; the farm still points at the old logo in the database.
            await _files.DeleteAsync(stored.Id, CancellationToken.None);
            throw;
        }

        // Only after the farm points at the new file: a failure above leaves the old logo working.
        if (previous is not null) await _files.DeleteAsync(previous, ct);

        return new FarmLogoDto(true, stored.ContentType, stored.Size, farm.LogoUpdatedAt);
    }
}
