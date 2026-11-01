using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Mapping;
using AgroMesh.Application.Services;
using MediatR;

namespace AgroMesh.Application.Commands.Invoice;

public record CreateInvoiceCommand(string FarmId, SaveInvoiceRequest Request) : IRequest<InvoiceDto>;

public class CreateInvoiceCommandHandler : IRequestHandler<CreateInvoiceCommand, InvoiceDto>
{
    private readonly IFarmRepository _farmRepo;
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly IInvoiceTemplateRegistry _templates;
    private readonly ICurrentUserAccessor _currentUser;

    public CreateInvoiceCommandHandler(IFarmRepository farmRepo, IInvoiceRepository invoiceRepo, IInvoiceTemplateRegistry templates, ICurrentUserAccessor currentUser)
    {
        _farmRepo = farmRepo;
        _invoiceRepo = invoiceRepo;
        _templates = templates;
        _currentUser = currentUser;
    }

    public async Task<InvoiceDto> Handle(CreateInvoiceCommand command, CancellationToken ct)
    {
        var farm = await FarmAccess.GetFarmAsync(_farmRepo, _currentUser, command.FarmId, ct);
        FarmAccess.RequireManager(_currentUser, "create invoices");

        var r = command.Request;
        var templateKey = InvoiceMapping.ResolveTemplateKey(_templates, r.TemplateKey);
        var date = r.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var dueDate = r.DueDate ?? InvoiceMapping.DefaultDueDate(date);

        var accountName = r.PaymentAccountName;
        var accountNumber = r.PaymentAccountNumber;
        var bankName = r.PaymentBankName;
        if (string.IsNullOrWhiteSpace(accountName) && string.IsNullOrWhiteSpace(accountNumber) && string.IsNullOrWhiteSpace(bankName))
        {
            // Copied, not linked: an invoice already sent keeps the account it was sent with.
            accountName = farm.BankAccountName;
            accountNumber = farm.BankAccountNumber;
            bankName = farm.BankName;
        }

        var details = InvoiceMapping.ToDetails(r, date, dueDate, templateKey, accountName, accountNumber, bankName);

        // Validate first (with a placeholder number), then reserve: a rejected invoice must not spend a number.
        var invoice = Domain.Entities.Invoice.Create(farm.Id, 1, details, _currentUser.UserId);
        invoice.AssignNumber(await _invoiceRepo.ReserveNextNumberAsync(farm.Id, ct));
        await _invoiceRepo.AddAsync(invoice, ct);

        return InvoiceMapping.ToDto(invoice);
    }
}
