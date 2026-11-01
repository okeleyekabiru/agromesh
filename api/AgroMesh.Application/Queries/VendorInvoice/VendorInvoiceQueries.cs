using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Mapping;
using AgroMesh.Application.Services;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Queries.VendorInvoice;

/// <summary>A farm's vendor invoices, newest first. OrderId narrows to one order and Status ("Paid"/"Unpaid") to one status.</summary>
public record GetVendorInvoicesQuery(string FarmId, string? OrderId, string? Status) : IRequest<List<VendorInvoiceDto>>;

public class GetVendorInvoicesQueryHandler : IRequestHandler<GetVendorInvoicesQuery, List<VendorInvoiceDto>>
{
    private readonly IFarmRepository _farmRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly IVendorInvoiceRepository _bills;
    private readonly ICurrentUserAccessor _currentUser;

    public GetVendorInvoicesQueryHandler(IFarmRepository farmRepo, IOrderRepository orderRepo, IVendorInvoiceRepository bills, ICurrentUserAccessor currentUser)
    {
        _farmRepo = farmRepo;
        _orderRepo = orderRepo;
        _bills = bills;
        _currentUser = currentUser;
    }

    public async Task<List<VendorInvoiceDto>> Handle(GetVendorInvoicesQuery query, CancellationToken ct)
    {
        var farm = await FarmAccess.GetFarmAsync(_farmRepo, _currentUser, query.FarmId, ct);

        if (query.OrderId is not null)
            VendorInvoiceMapping.EnsureOrderOfFarm(await _orderRepo.GetByIdAsync(query.OrderId, ct), farm.Id, query.OrderId);

        VendorInvoiceStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<VendorInvoiceStatus>(query.Status, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
                throw new DomainException("Status must be 'Paid' or 'Unpaid'.");
            status = parsed;
        }

        var bills = await _bills.GetByFarmIdAsync(farm.Id, query.OrderId, status, ct);

        // One query for the farm's orders, not one per bill.
        var orders = (await _orderRepo.GetByFarmIdAsync(farm.Id, null, ct)).ToDictionary(o => o.Id);
        var today = VendorInvoiceMapping.Today();
        return bills.Select(b => VendorInvoiceMapping.ToDto(b, orders.GetValueOrDefault(b.OrderId), today)).ToList();
    }
}

public record GetVendorInvoiceQuery(string FarmId, string VendorInvoiceId) : IRequest<VendorInvoiceDto>;

public class GetVendorInvoiceQueryHandler : IRequestHandler<GetVendorInvoiceQuery, VendorInvoiceDto>
{
    private readonly IFarmRepository _farmRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly IVendorInvoiceRepository _bills;
    private readonly ICurrentUserAccessor _currentUser;

    public GetVendorInvoiceQueryHandler(IFarmRepository farmRepo, IOrderRepository orderRepo, IVendorInvoiceRepository bills, ICurrentUserAccessor currentUser)
    {
        _farmRepo = farmRepo;
        _orderRepo = orderRepo;
        _bills = bills;
        _currentUser = currentUser;
    }

    public async Task<VendorInvoiceDto> Handle(GetVendorInvoiceQuery query, CancellationToken ct)
    {
        var farm = await FarmAccess.GetFarmAsync(_farmRepo, _currentUser, query.FarmId, ct);
        var bill = VendorInvoiceMapping.EnsureFarmScoped(await _bills.GetByIdAsync(query.VendorInvoiceId, ct), farm.Id, query.VendorInvoiceId);
        var order = await _orderRepo.GetByIdAsync(bill.OrderId, ct);
        return VendorInvoiceMapping.ToDto(bill, order, VendorInvoiceMapping.Today());
    }
}

public record DownloadVendorInvoiceFileQuery(string FarmId, string VendorInvoiceId) : IRequest<VendorInvoiceFile>;

public class DownloadVendorInvoiceFileQueryHandler : IRequestHandler<DownloadVendorInvoiceFileQuery, VendorInvoiceFile>
{
    private readonly IFarmRepository _farmRepo;
    private readonly IVendorInvoiceRepository _bills;
    private readonly IStoredFileRepository _files;
    private readonly ICurrentUserAccessor _currentUser;

    public DownloadVendorInvoiceFileQueryHandler(IFarmRepository farmRepo, IVendorInvoiceRepository bills, IStoredFileRepository files, ICurrentUserAccessor currentUser)
    {
        _farmRepo = farmRepo;
        _bills = bills;
        _files = files;
        _currentUser = currentUser;
    }

    public async Task<VendorInvoiceFile> Handle(DownloadVendorInvoiceFileQuery query, CancellationToken ct)
    {
        var farm = await FarmAccess.GetFarmAsync(_farmRepo, _currentUser, query.FarmId, ct);
        var bill = VendorInvoiceMapping.EnsureFarmScoped(await _bills.GetByIdAsync(query.VendorInvoiceId, ct), farm.Id, query.VendorInvoiceId);
        var file = await _files.GetByIdAsync(bill.FileId, ct);
        if (file is null || file.FarmId != farm.Id) throw new NotFoundException("Vendor invoice file", query.VendorInvoiceId);
        return new VendorInvoiceFile(file.Content, bill.ContentType, bill.FileName);
    }
}
