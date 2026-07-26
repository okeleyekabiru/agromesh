using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using MediatR;

namespace AgroMesh.Application.Queries.Supplier;

public record GetSuppliersQuery(string FarmId) : IRequest<List<SupplierDto>>;

public class GetSuppliersQueryHandler : IRequestHandler<GetSuppliersQuery, List<SupplierDto>>
{
    private readonly ISupplierRepository _supplierRepo;

    public GetSuppliersQueryHandler(ISupplierRepository supplierRepo) => _supplierRepo = supplierRepo;

    public async Task<List<SupplierDto>> Handle(GetSuppliersQuery query, CancellationToken ct)
    {
        var suppliers = await _supplierRepo.GetByFarmIdAsync(query.FarmId, ct);
        return suppliers.Select(s => new SupplierDto(s.Id, s.FarmId, s.Name, s.Phone, s.Email)).ToList();
    }
}
