using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Mapping;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Queries.Order;

public record GenerateOrderInvoiceQuery(string FarmId, string OrderId) : IRequest<byte[]>;

public class GenerateOrderInvoiceQueryHandler : IRequestHandler<GenerateOrderInvoiceQuery, byte[]>
{
    private readonly IOrderRepository _orderRepo;
    private readonly ISupplierRepository _supplierRepo;
    private readonly IInventoryRepository _inventoryRepo;
    private readonly IFarmRepository _farmRepo;
    private readonly IOrderInvoiceService _invoiceService;

    public GenerateOrderInvoiceQueryHandler(
        IOrderRepository orderRepo, ISupplierRepository supplierRepo, IInventoryRepository inventoryRepo,
        IFarmRepository farmRepo, IOrderInvoiceService invoiceService)
    {
        _orderRepo = orderRepo;
        _supplierRepo = supplierRepo;
        _inventoryRepo = inventoryRepo;
        _farmRepo = farmRepo;
        _invoiceService = invoiceService;
    }

    public async Task<byte[]> Handle(GenerateOrderInvoiceQuery query, CancellationToken ct)
    {
        var order = await _orderRepo.GetByIdAsync(query.OrderId, ct)
            ?? throw new DomainException($"Order '{query.OrderId}' not found.");
        OrderDtoMapping.EnsureFarmScoped(order, query.FarmId);

        var supplier = await OrderDtoMapping.ResolveSupplierAsync(_supplierRepo, query.FarmId, order.SupplierId, ct);
        var itemsById = await OrderDtoMapping.ResolveLineItemsAsync(_inventoryRepo, query.FarmId, order.LineItems, ct);
        var farm = await _farmRepo.GetByIdAsync(query.FarmId, ct)
            ?? throw new DomainException($"Farm '{query.FarmId}' not found.");

        var orderDto = OrderDtoMapping.ToDto(order, supplier, itemsById);
        return _invoiceService.Generate(orderDto, farm);
    }
}
