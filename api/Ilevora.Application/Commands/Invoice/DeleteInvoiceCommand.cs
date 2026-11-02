using Ilevora.Application.Interfaces;
using Ilevora.Application.Mapping;
using Ilevora.Application.Services;
using MediatR;

namespace Ilevora.Application.Commands.Invoice;

public record DeleteInvoiceCommand(string FarmId, string InvoiceId) : IRequest;

public class DeleteInvoiceCommandHandler : IRequestHandler<DeleteInvoiceCommand>
{
    private readonly IFarmRepository _farmRepo;
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public DeleteInvoiceCommandHandler(IFarmRepository farmRepo, IInvoiceRepository invoiceRepo, ICurrentUserAccessor currentUser)
    {
        _farmRepo = farmRepo;
        _invoiceRepo = invoiceRepo;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteInvoiceCommand command, CancellationToken ct)
    {
        var farm = await FarmAccess.GetFarmAsync(_farmRepo, _currentUser, command.FarmId, ct);
        FarmAccess.RequireManager(_currentUser, "delete invoices");
        var invoice = InvoiceMapping.EnsureFarmScoped(await _invoiceRepo.GetByIdAsync(command.InvoiceId, ct), farm.Id, command.InvoiceId);
        await _invoiceRepo.DeleteAsync(invoice, ct);
    }
}
