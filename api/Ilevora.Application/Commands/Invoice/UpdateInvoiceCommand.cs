using Ilevora.Application.DTOs;
using Ilevora.Application.Interfaces;
using Ilevora.Application.Mapping;
using Ilevora.Application.Services;
using MediatR;

namespace Ilevora.Application.Commands.Invoice;

public record UpdateInvoiceCommand(string FarmId, string InvoiceId, SaveInvoiceRequest Request) : IRequest<InvoiceDto>;

public class UpdateInvoiceCommandHandler : IRequestHandler<UpdateInvoiceCommand, InvoiceDto>
{
    private readonly IFarmRepository _farmRepo;
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly IInvoiceTemplateRegistry _templates;
    private readonly ICurrentUserAccessor _currentUser;

    public UpdateInvoiceCommandHandler(IFarmRepository farmRepo, IInvoiceRepository invoiceRepo, IInvoiceTemplateRegistry templates, ICurrentUserAccessor currentUser)
    {
        _farmRepo = farmRepo;
        _invoiceRepo = invoiceRepo;
        _templates = templates;
        _currentUser = currentUser;
    }

    public async Task<InvoiceDto> Handle(UpdateInvoiceCommand command, CancellationToken ct)
    {
        var farm = await FarmAccess.GetFarmAsync(_farmRepo, _currentUser, command.FarmId, ct);
        FarmAccess.RequireManager(_currentUser, "change invoices");
        var invoice = InvoiceMapping.EnsureFarmScoped(await _invoiceRepo.GetByIdAsync(command.InvoiceId, ct), farm.Id, command.InvoiceId);

        var r = command.Request;
        var templateKey = InvoiceMapping.ResolveTemplateKey(_templates, r.TemplateKey ?? invoice.TemplateKey);
        var date = r.Date ?? invoice.Date;
        var dueDate = r.DueDate ?? (invoice.DueDate >= date ? invoice.DueDate : InvoiceMapping.DefaultDueDate(date));

        invoice.Update(InvoiceMapping.ToDetails(r, date, dueDate, templateKey, r.PaymentAccountName, r.PaymentAccountNumber, r.PaymentBankName));
        await _invoiceRepo.UpdateAsync(invoice, ct);

        return InvoiceMapping.ToDto(invoice, InvoiceMapping.ResolveTheme(invoice, farm, _templates));
    }
}
