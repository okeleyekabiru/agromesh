using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Commands.Inventory;

public static class PriceChangeMapping
{
    public static PriceChangeRequestDto ToDto(PriceChangeRequest r, InventoryItem? item) => new(
        r.Id, r.FarmId, r.InventoryItemId,
        item?.Name ?? "(deleted item)", item?.Unit ?? "",
        item?.UnitCost ?? 0, item?.PackLabel ?? "unit", item?.PackSize ?? 1,
        r.PackLabel, r.PackSize, r.PricePerPack, r.PricePerUnit,
        r.BackfillActivities, r.Notes, r.Status.ToString(),
        r.RequestedByUserId, r.ReviewedByUserId, r.ReviewedAt, r.ReviewNotes, r.CreatedAt);
}

/// <summary>
/// Asks for a price for a stock item. The price decides the cost of every activity that uses the item, so it is NOT applied here:
/// it waits in the queue until an admin approves it (see <see cref="ApprovePriceChangeCommand"/>).
/// </summary>
public record RequestItemPricingCommand(string FarmId, string ItemId, SetItemPricingRequest Request, string? Notes = null) : IRequest<PriceChangeRequestDto>;

public class RequestItemPricingCommandHandler : IRequestHandler<RequestItemPricingCommand, PriceChangeRequestDto>
{
    private readonly IInventoryRepository _inventoryRepo;
    private readonly IPriceChangeRequestRepository _requestRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public RequestItemPricingCommandHandler(
        IInventoryRepository inventoryRepo, IPriceChangeRequestRepository requestRepo, ICurrentUserAccessor currentUser)
    {
        _inventoryRepo = inventoryRepo;
        _requestRepo = requestRepo;
        _currentUser = currentUser;
    }

    public async Task<PriceChangeRequestDto> Handle(RequestItemPricingCommand command, CancellationToken ct)
    {
        var item = await _inventoryRepo.GetByIdAsync(command.ItemId, ct);
        if (item is null || item.FarmId != command.FarmId)
            throw new DomainException($"Inventory item '{command.ItemId}' not found.");

        var userId = _currentUser.UserId ?? throw new DomainException("Could not identify the requesting user.");
        var req = command.Request;

        // One open request per item: a newer ask replaces the waiting one rather than queueing two prices for the same stock.
        foreach (var open in (await _requestRepo.GetByFarmIdAsync(command.FarmId, PriceChangeStatus.Pending, ct))
                     .Where(r => r.InventoryItemId == item.Id))
        {
            open.Reject(userId, "Replaced by a newer price request.");
            await _requestRepo.UpdateAsync(open, ct);
        }

        var request = PriceChangeRequest.Create(
            command.FarmId, item.Id, req.PackLabel, req.PackSize, req.PricePerPack, req.BackfillActivities, command.Notes, userId);
        await _requestRepo.AddAsync(request, ct);
        return PriceChangeMapping.ToDto(request, item);
    }
}

/// <summary>Admin only. Applies the requested price and pack size to the item (and backfills past activities when asked).</summary>
public record ApprovePriceChangeCommand(string FarmId, string RequestId, string? ReviewNotes) : IRequest<PriceChangeRequestDto>;

public class ApprovePriceChangeCommandHandler : IRequestHandler<ApprovePriceChangeCommand, PriceChangeRequestDto>
{
    private readonly IPriceChangeRequestRepository _requestRepo;
    private readonly IInventoryRepository _inventoryRepo;
    private readonly IActivityRepository _activityRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public ApprovePriceChangeCommandHandler(
        IPriceChangeRequestRepository requestRepo, IInventoryRepository inventoryRepo, IActivityRepository activityRepo,
        ICurrentUserAccessor currentUser)
    {
        _requestRepo = requestRepo;
        _inventoryRepo = inventoryRepo;
        _activityRepo = activityRepo;
        _currentUser = currentUser;
    }

    public async Task<PriceChangeRequestDto> Handle(ApprovePriceChangeCommand command, CancellationToken ct)
    {
        // Role first, so a non-admin learns nothing about whether the request exists.
        if (!_currentUser.IsAdmin)
            throw new ForbiddenException("Only an admin can approve a price change.");

        var request = await _requestRepo.GetByIdAsync(command.RequestId, ct);
        if (request is null || request.FarmId != command.FarmId)
            throw new DomainException($"Price change '{command.RequestId}' not found.");
        // Checked before anything is applied: approving twice must never apply the price twice.
        if (request.Status != PriceChangeStatus.Pending)
            throw new DomainException($"This price change has already been {request.Status.ToString().ToLowerInvariant()}.");

        var reviewerId = _currentUser.UserId ?? throw new DomainException("Could not identify the reviewing user.");

        // The pricing itself (pack, unit cost and the optional backfill) is the existing apply step.
        await new SetItemPricingCommandHandler(_inventoryRepo, _activityRepo).Handle(
            new SetItemPricingCommand(command.FarmId, request.InventoryItemId,
                new SetItemPricingRequest(request.PackSize, request.PackLabel, request.PricePerPack, request.BackfillActivities)), ct);

        request.Approve(reviewerId, command.ReviewNotes);
        await _requestRepo.UpdateAsync(request, ct);

        var item = await _inventoryRepo.GetByIdAsync(request.InventoryItemId, ct);
        return PriceChangeMapping.ToDto(request, item);
    }
}

/// <summary>Admin only. Leaves the item's price as it is.</summary>
public record RejectPriceChangeCommand(string FarmId, string RequestId, string? ReviewNotes) : IRequest<PriceChangeRequestDto>;

public class RejectPriceChangeCommandHandler : IRequestHandler<RejectPriceChangeCommand, PriceChangeRequestDto>
{
    private readonly IPriceChangeRequestRepository _requestRepo;
    private readonly IInventoryRepository _inventoryRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public RejectPriceChangeCommandHandler(
        IPriceChangeRequestRepository requestRepo, IInventoryRepository inventoryRepo, ICurrentUserAccessor currentUser)
    {
        _requestRepo = requestRepo;
        _inventoryRepo = inventoryRepo;
        _currentUser = currentUser;
    }

    public async Task<PriceChangeRequestDto> Handle(RejectPriceChangeCommand command, CancellationToken ct)
    {
        if (!_currentUser.IsAdmin)
            throw new ForbiddenException("Only an admin can reject a price change.");

        var request = await _requestRepo.GetByIdAsync(command.RequestId, ct);
        if (request is null || request.FarmId != command.FarmId)
            throw new DomainException($"Price change '{command.RequestId}' not found.");

        var reviewerId = _currentUser.UserId ?? throw new DomainException("Could not identify the reviewing user.");
        request.Reject(reviewerId, command.ReviewNotes);
        await _requestRepo.UpdateAsync(request, ct);

        var item = await _inventoryRepo.GetByIdAsync(request.InventoryItemId, ct);
        return PriceChangeMapping.ToDto(request, item);
    }
}
