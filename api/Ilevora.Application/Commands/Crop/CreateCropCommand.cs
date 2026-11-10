using Ilevora.Application.DTOs;
using Ilevora.Application.Interfaces;
using Ilevora.Application.Services;
using Ilevora.Domain.Crops;
using Ilevora.Domain.Exceptions;
using MediatR;

namespace Ilevora.Application.Commands.Crop;

public record CreateCropCommand(string FarmId, CreateCropRequest Request) : IRequest<CropDto>;

public class CreateCropCommandHandler : IRequestHandler<CreateCropCommand, CropDto>
{
    private readonly ICropRepository _cropRepo;
    private readonly IFarmRepository _farmRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public CreateCropCommandHandler(ICropRepository cropRepo, IFarmRepository farmRepo, ICurrentUserAccessor currentUser)
    {
        _cropRepo = cropRepo;
        _farmRepo = farmRepo;
        _currentUser = currentUser;
    }

    public async Task<CropDto> Handle(CreateCropCommand command, CancellationToken ct)
    {
        var farm = await _farmRepo.GetByIdAsync(command.FarmId, ct)
            ?? throw new NotFoundException("Farm", command.FarmId);
        if (!string.IsNullOrEmpty(_currentUser.CompanyId) && farm.CompanyId != _currentUser.CompanyId)
            throw new NotFoundException("Farm", command.FarmId);

        var crop = Domain.Entities.Crop.CreateCustom(farm.Id, command.Request.Name ?? "");

        var taken = BuiltInCrop.All.Any(b => string.Equals(b.Name, crop.Name, StringComparison.OrdinalIgnoreCase))
            || (await _cropRepo.GetByFarmIdAsync(farm.Id, ct)).Any(c => string.Equals(c.Name, crop.Name, StringComparison.OrdinalIgnoreCase));
        if (taken)
            throw new DomainException($"{crop.Name} is already in the crop list.");

        await _cropRepo.AddAsync(crop, ct);
        return new CropDto(crop.Id, crop.Name, false, CropResolver.WireOrdinal(crop.Id));
    }
}
