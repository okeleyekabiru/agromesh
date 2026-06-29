using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.Utilities;
using MediatR;

namespace AgroMesh.Application.Commands.Section;

public record CreateBlockCommand(string SectionId, CreateBlockRequest Request) : IRequest<BlockDto>;

public class CreateBlockCommandHandler : IRequestHandler<CreateBlockCommand, BlockDto>
{
    private readonly IBlockRepository _blockRepo;
    private readonly ISectionRepository _sectionRepo;
    private readonly IFarmRepository _farmRepo;

    public CreateBlockCommandHandler(IBlockRepository blockRepo, ISectionRepository sectionRepo, IFarmRepository farmRepo)
    {
        _blockRepo = blockRepo;
        _sectionRepo = sectionRepo;
        _farmRepo = farmRepo;
    }

    public async Task<BlockDto> Handle(CreateBlockCommand command, CancellationToken ct)
    {
        var section = await _sectionRepo.GetByIdAsync(command.SectionId, ct)
            ?? throw new DomainException($"Section '{command.SectionId}' not found.");
        var farm = await _farmRepo.GetByIdAsync(section.FarmId, ct)
            ?? throw new DomainException($"Farm '{section.FarmId}' not found.");

        var req = command.Request;
        var hectares = AreaConverter.ToHectares(req.AreaValue, farm.PreferredAreaUnit);

        var block = Domain.Entities.Block.Create(command.SectionId, section.FarmId, req.Name, hectares, req.TreeCount);
        await _blockRepo.AddAsync(block, ct);

        return new BlockDto(block.Id, block.SectionId, block.Name, AreaDto.From(block.Hectares, farm.PreferredAreaUnit), block.TreeCount);
    }
}
