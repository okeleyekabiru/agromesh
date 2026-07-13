using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using MediatR;

namespace AgroMesh.Application.Queries.Inventory;

public record GetInventoryCategoriesQuery(string FarmId) : IRequest<List<InventoryCategoryDto>>;

public class GetInventoryCategoriesQueryHandler : IRequestHandler<GetInventoryCategoriesQuery, List<InventoryCategoryDto>>
{
    private readonly IInventoryCategoryRepository _categoryRepo;

    public GetInventoryCategoriesQueryHandler(IInventoryCategoryRepository categoryRepo) => _categoryRepo = categoryRepo;

    public async Task<List<InventoryCategoryDto>> Handle(GetInventoryCategoriesQuery query, CancellationToken ct)
    {
        var categories = await _categoryRepo.GetByFarmIdAsync(query.FarmId, ct);
        return categories.Select(c => new InventoryCategoryDto(c.Id, c.FarmId, c.Name, c.IsDefault)).ToList();
    }
}
