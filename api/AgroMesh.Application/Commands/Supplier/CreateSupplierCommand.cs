using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using MediatR;

namespace AgroMesh.Application.Commands.Supplier;

public record CreateSupplierCommand(string FarmId, CreateSupplierRequest Request) : IRequest<SupplierDto>;

public class CreateSupplierCommandHandler : IRequestHandler<CreateSupplierCommand, SupplierDto>
{
    private readonly ISupplierRepository _supplierRepo;

    public CreateSupplierCommandHandler(ISupplierRepository supplierRepo) => _supplierRepo = supplierRepo;

    public async Task<SupplierDto> Handle(CreateSupplierCommand command, CancellationToken ct)
    {
        var supplier = Domain.Entities.Supplier.Create(command.FarmId, command.Request.Name, command.Request.Phone, command.Request.Email);
        await _supplierRepo.AddAsync(supplier, ct);

        return new SupplierDto(supplier.Id, supplier.FarmId, supplier.Name, supplier.Phone, supplier.Email);
    }
}
