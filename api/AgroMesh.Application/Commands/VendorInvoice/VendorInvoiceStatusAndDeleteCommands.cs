using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Mapping;
using AgroMesh.Application.Services;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Commands.VendorInvoice;

/// <summary>Records whether the farm has settled a vendor's bill. Only the bill's own status changes; nothing is posted to finance.</summary>
public record SetVendorInvoiceStatusCommand(string FarmId, string VendorInvoiceId, SetVendorInvoiceStatusRequest Request) : IRequest<VendorInvoiceDto>;

public class SetVendorInvoiceStatusCommandHandler : IRequestHandler<SetVendorInvoiceStatusCommand, VendorInvoiceDto>
{
    private readonly IFarmRepository _farmRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly IVendorInvoiceRepository _bills;
    private readonly ICurrentUserAccessor _currentUser;

    public SetVendorInvoiceStatusCommandHandler(IFarmRepository farmRepo, IOrderRepository orderRepo, IVendorInvoiceRepository bills, ICurrentUserAccessor currentUser)
    {
        _farmRepo = farmRepo;
        _orderRepo = orderRepo;
        _bills = bills;
        _currentUser = currentUser;
    }

    public async Task<VendorInvoiceDto> Handle(SetVendorInvoiceStatusCommand command, CancellationToken ct)
    {
        var farm = await FarmAccess.GetFarmAsync(_farmRepo, _currentUser, command.FarmId, ct);
        FarmAccess.RequireManager(_currentUser, "change a vendor invoice's status");
        var bill = VendorInvoiceMapping.EnsureFarmScoped(await _bills.GetByIdAsync(command.VendorInvoiceId, ct), farm.Id, command.VendorInvoiceId);

        if (!Enum.TryParse<VendorInvoiceStatus>(command.Request.Status, ignoreCase: true, out var status) || !Enum.IsDefined(status))
            throw new DomainException("Status must be 'Paid' or 'Unpaid'.");

        if (status == VendorInvoiceStatus.Paid) bill.MarkPaid(command.Request.PaidAt);
        else bill.MarkUnpaid();
        await _bills.UpdateAsync(bill, ct);

        var order = await _orderRepo.GetByIdAsync(bill.OrderId, ct);
        return VendorInvoiceMapping.ToDto(bill, order, VendorInvoiceMapping.Today());
    }
}

public record DeleteVendorInvoiceCommand(string FarmId, string VendorInvoiceId) : IRequest;

public class DeleteVendorInvoiceCommandHandler : IRequestHandler<DeleteVendorInvoiceCommand>
{
    private readonly IFarmRepository _farmRepo;
    private readonly IVendorInvoiceRepository _bills;
    private readonly IStoredFileRepository _files;
    private readonly ICurrentUserAccessor _currentUser;

    public DeleteVendorInvoiceCommandHandler(IFarmRepository farmRepo, IVendorInvoiceRepository bills, IStoredFileRepository files, ICurrentUserAccessor currentUser)
    {
        _farmRepo = farmRepo;
        _bills = bills;
        _files = files;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteVendorInvoiceCommand command, CancellationToken ct)
    {
        var farm = await FarmAccess.GetFarmAsync(_farmRepo, _currentUser, command.FarmId, ct);
        FarmAccess.RequireManager(_currentUser, "delete a vendor invoice");
        var bill = VendorInvoiceMapping.EnsureFarmScoped(await _bills.GetByIdAsync(command.VendorInvoiceId, ct), farm.Id, command.VendorInvoiceId);

        await _bills.DeleteAsync(bill, ct);
        await _files.DeleteAsync(bill.FileId, ct);
    }
}
