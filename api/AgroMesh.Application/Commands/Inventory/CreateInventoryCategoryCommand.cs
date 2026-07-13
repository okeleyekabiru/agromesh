using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using MediatR;

namespace AgroMesh.Application.Commands.Inventory;

public record CreateInventoryCategoryCommand(string FarmId, CreateInventoryCategoryRequest Request) : IRequest<InventoryCategoryDto>;

public class CreateInventoryCategoryCommandHandler : IRequestHandler<CreateInventoryCategoryCommand, InventoryCategoryDto>
{
    private readonly IInventoryCategoryRepository _categoryRepo;

    public CreateInventoryCategoryCommandHandler(IInventoryCategoryRepository categoryRepo) => _categoryRepo = categoryRepo;

    public async Task<InventoryCategoryDto> Handle(CreateInventoryCategoryCommand command, CancellationToken ct)
    {
        var category = Domain.Entities.InventoryCategory.Create(command.FarmId, command.Request.Name);
        await _categoryRepo.AddAsync(category, ct);

        return new InventoryCategoryDto(category.Id, category.FarmId, category.Name, category.IsDefault);
    }
}
