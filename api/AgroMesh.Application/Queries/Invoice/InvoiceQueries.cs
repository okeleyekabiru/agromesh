using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Mapping;
using AgroMesh.Application.Services;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Queries.Invoice;

public record GetInvoicesQuery(string FarmId) : IRequest<List<InvoiceSummaryDto>>;

public class GetInvoicesQueryHandler : IRequestHandler<GetInvoicesQuery, List<InvoiceSummaryDto>>
{
    private readonly IFarmRepository _farmRepo;
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public GetInvoicesQueryHandler(IFarmRepository farmRepo, IInvoiceRepository invoiceRepo, ICurrentUserAccessor currentUser)
    {
        _farmRepo = farmRepo;
        _invoiceRepo = invoiceRepo;
        _currentUser = currentUser;
    }

    public async Task<List<InvoiceSummaryDto>> Handle(GetInvoicesQuery query, CancellationToken ct)
    {
        var farm = await FarmAccess.GetFarmAsync(_farmRepo, _currentUser, query.FarmId, ct);
        return (await _invoiceRepo.GetByFarmIdAsync(farm.Id, ct)).Select(InvoiceMapping.ToSummary).ToList();
    }
}

public record GetInvoiceQuery(string FarmId, string InvoiceId) : IRequest<InvoiceDto>;

public class GetInvoiceQueryHandler : IRequestHandler<GetInvoiceQuery, InvoiceDto>
{
    private readonly IFarmRepository _farmRepo;
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public GetInvoiceQueryHandler(IFarmRepository farmRepo, IInvoiceRepository invoiceRepo, ICurrentUserAccessor currentUser)
    {
        _farmRepo = farmRepo;
        _invoiceRepo = invoiceRepo;
        _currentUser = currentUser;
    }

    public async Task<InvoiceDto> Handle(GetInvoiceQuery query, CancellationToken ct)
    {
        var farm = await FarmAccess.GetFarmAsync(_farmRepo, _currentUser, query.FarmId, ct);
        var invoice = InvoiceMapping.EnsureFarmScoped(await _invoiceRepo.GetByIdAsync(query.InvoiceId, ct), farm.Id, query.InvoiceId);
        return InvoiceMapping.ToDto(invoice);
    }
}

public record GetInvoiceTemplatesQuery(string FarmId) : IRequest<List<InvoiceTemplateDto>>;

public class GetInvoiceTemplatesQueryHandler : IRequestHandler<GetInvoiceTemplatesQuery, List<InvoiceTemplateDto>>
{
    private readonly IFarmRepository _farmRepo;
    private readonly IInvoiceTemplateRegistry _templates;
    private readonly ICurrentUserAccessor _currentUser;

    public GetInvoiceTemplatesQueryHandler(IFarmRepository farmRepo, IInvoiceTemplateRegistry templates, ICurrentUserAccessor currentUser)
    {
        _farmRepo = farmRepo;
        _templates = templates;
        _currentUser = currentUser;
    }

    public async Task<List<InvoiceTemplateDto>> Handle(GetInvoiceTemplatesQuery query, CancellationToken ct)
    {
        await FarmAccess.GetFarmAsync(_farmRepo, _currentUser, query.FarmId, ct);
        return _templates.Templates
            .Select(t => new InvoiceTemplateDto(t.Key, t.Name, t.Description, t.Key == Domain.Entities.InvoiceTemplateKeys.Default))
            .ToList();
    }
}

public record GenerateInvoicePdfQuery(string FarmId, string InvoiceId) : IRequest<InvoicePdfFile>;

public class GenerateInvoicePdfQueryHandler : IRequestHandler<GenerateInvoicePdfQuery, InvoicePdfFile>
{
    private readonly IFarmRepository _farmRepo;
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly IStoredFileRepository _files;
    private readonly IInvoiceTemplateRegistry _templates;
    private readonly ICurrentUserAccessor _currentUser;

    public GenerateInvoicePdfQueryHandler(IFarmRepository farmRepo, IInvoiceRepository invoiceRepo, IStoredFileRepository files,
        IInvoiceTemplateRegistry templates, ICurrentUserAccessor currentUser)
    {
        _farmRepo = farmRepo;
        _invoiceRepo = invoiceRepo;
        _files = files;
        _templates = templates;
        _currentUser = currentUser;
    }

    public async Task<InvoicePdfFile> Handle(GenerateInvoicePdfQuery query, CancellationToken ct)
    {
        var farm = await FarmAccess.GetFarmAsync(_farmRepo, _currentUser, query.FarmId, ct);
        var invoice = InvoiceMapping.EnsureFarmScoped(await _invoiceRepo.GetByIdAsync(query.InvoiceId, ct), farm.Id, query.InvoiceId);

        if (!_templates.Exists(invoice.TemplateKey))
            throw new DomainException($"The invoice template '{invoice.TemplateKey}' is no longer available. Edit the invoice and choose another.");

        // A logo that cannot be found just means a plain header: the invoice must still print.
        byte[]? logo = null;
        if (farm.LogoFileId is not null)
        {
            var file = await _files.GetByIdAsync(farm.LogoFileId, ct);
            if (file is not null && file.FarmId == farm.Id) logo = file.Content;
        }

        var issuer = new InvoiceIssuer(farm.Name, farm.Address, farm.Phone, farm.Email, farm.Website, logo);
        var bytes = _templates.Render(invoice.TemplateKey, new InvoiceRenderModel(invoice, issuer));
        return new InvoicePdfFile(bytes, $"{invoice.Reference}.pdf");
    }
}
