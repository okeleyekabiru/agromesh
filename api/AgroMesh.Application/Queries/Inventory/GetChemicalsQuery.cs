using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using MediatR;

namespace AgroMesh.Application.Queries.Inventory;

public record GetChemicalsQuery : IRequest<List<ChemicalProductDto>>;

public class GetChemicalsQueryHandler : IRequestHandler<GetChemicalsQuery, List<ChemicalProductDto>>
{
    private readonly IChemicalProductRepository _chemicalRepo;

    public GetChemicalsQueryHandler(IChemicalProductRepository chemicalRepo) => _chemicalRepo = chemicalRepo;

    public async Task<List<ChemicalProductDto>> Handle(GetChemicalsQuery query, CancellationToken ct)
    {
        var chemicals = await _chemicalRepo.GetAllActiveAsync(ct);
        return chemicals.Select(c => new ChemicalProductDto(c.Id, c.Name, c.Manufacturer, c.ChemType, c.TypicalIntervalDays, c.Active)).ToList();
    }
}
