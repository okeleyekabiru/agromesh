using Ilevora.Application.DTOs;
using Ilevora.Application.Interfaces;
using Ilevora.Application.Mapping;
using Ilevora.Application.Services;
using Ilevora.Domain.Entities;
using Ilevora.Domain.Enums;
using Ilevora.Domain.Exceptions;
using MediatR;

namespace Ilevora.Application.Commands.VendorInvoice;

/// <summary>Attaches the invoice a vendor sent to a completed (Received) order of this farm.</summary>
public record UploadVendorInvoiceCommand(string FarmId, string OrderId, byte[] Content, string? FileName, string? ContentType,
    UploadVendorInvoiceFields Fields) : IRequest<VendorInvoiceDto>;

public class UploadVendorInvoiceCommandHandler : IRequestHandler<UploadVendorInvoiceCommand, VendorInvoiceDto>
{
    private readonly IFarmRepository _farmRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly ISupplierRepository _supplierRepo;
    private readonly IStoredFileRepository _files;
    private readonly IVendorInvoiceRepository _bills;
    private readonly ICurrentUserAccessor _currentUser;

    public UploadVendorInvoiceCommandHandler(IFarmRepository farmRepo, IOrderRepository orderRepo, ISupplierRepository supplierRepo,
        IStoredFileRepository files, IVendorInvoiceRepository bills, ICurrentUserAccessor currentUser)
    {
        _farmRepo = farmRepo;
        _orderRepo = orderRepo;
        _supplierRepo = supplierRepo;
        _files = files;
        _bills = bills;
        _currentUser = currentUser;
    }

    public async Task<VendorInvoiceDto> Handle(UploadVendorInvoiceCommand command, CancellationToken ct)
    {
        var farm = await FarmAccess.GetFarmAsync(_farmRepo, _currentUser, command.FarmId, ct);
        if (!FarmAccess.CanUpload(_currentUser.Role))
            throw new ForbiddenException("Only an admin, farm manager or supervisor can attach a vendor invoice.");

        var order = VendorInvoiceMapping.EnsureOrderOfFarm(await _orderRepo.GetByIdAsync(command.OrderId, ct), farm.Id, command.OrderId);
        if (order.Status != OrderStatus.Received)
            throw new DomainException($"A vendor invoice can only be attached to a Received order. This order is {order.Status}.");

        var supplier = await _supplierRepo.GetByIdAsync(order.SupplierId, ct)
            ?? throw new NotFoundException("Supplier", order.SupplierId);

        var upload = UploadedFileValidator.Validate(command.Content, command.ContentType, UploadRules.VendorInvoice);
        var fileName = UploadedFileValidator.SafeFileName(command.FileName, "invoice" + upload.Extension);

        var f = command.Fields;
        var details = new VendorInvoiceDetails(
            string.IsNullOrWhiteSpace(f.VendorName) ? supplier.Name : f.VendorName,
            f.InvoiceNumber, f.Amount, f.InvoiceDate, f.DueDate, f.Notes);

        // Build the records first: any rule violation is raised before a file is stored.
        var file = StoredFile.Create(farm.Id, fileName, upload.ContentType, command.Content);
        var bill = Domain.Entities.VendorInvoice.Create(farm.Id, order.Id, order.SupplierId, file.Id, fileName, upload.ContentType,
            file.Size, details, _currentUser.UserId);

        if (await _bills.ExistsAsync(farm.Id, order.SupplierId, bill.InvoiceNumber, ct))
            throw new DomainException($"Invoice '{bill.InvoiceNumber}' from {supplier.Name} has already been uploaded.");

        await _files.AddAsync(file, ct);
        try
        {
            await _bills.AddAsync(bill, ct);
        }
        catch (DuplicateVendorInvoiceException)
        {
            // Another upload of the same number won the race between the check above and the insert.
            await _files.DeleteAsync(file.Id, CancellationToken.None);
            throw new DomainException($"Invoice '{bill.InvoiceNumber}' from {supplier.Name} has already been uploaded.");
        }
        catch
        {
            // Do not leave an orphaned file behind when the record could not be saved.
            await _files.DeleteAsync(file.Id, CancellationToken.None);
            throw;
        }

        return VendorInvoiceMapping.ToDto(bill, order, VendorInvoiceMapping.Today());
    }
}
