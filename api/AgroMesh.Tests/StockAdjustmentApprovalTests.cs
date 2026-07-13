using AgroMesh.Application.Commands.Inventory;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Queries.Inventory;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;

namespace AgroMesh.Tests;

/// <summary>
/// Handler tests for the Phase 3 approval workflow — extends this repo's actual precedent for
/// testing handlers that depend on ICurrentUserAccessor (BoundarySubmissionIdempotencyTests.cs's
/// hand-rolled fakes, no mocking library). The Member-role cases here are this repo's only proof of
/// the admin gate itself, since there's no live second-user-per-company story to test against in
/// e2e (see spec.md Non-goals).
/// </summary>
public class StockAdjustmentApprovalTests
{
    private class FakeInventoryRepository : IInventoryRepository
    {
        public List<InventoryItem> Items = [];
        public List<StockMovement> Movements = [];
        public int UpdateCallCount;
        public int AddMovementCallCount;

        public Task<InventoryItem?> GetByIdAsync(string id, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(i => i.Id == id));

        public Task<List<InventoryItem>> GetByFarmIdAsync(string farmId, CancellationToken ct = default) =>
            Task.FromResult(Items.Where(i => i.FarmId == farmId).ToList());

        public Task<List<InventoryItem>> GetLowStockAsync(string farmId, CancellationToken ct = default) =>
            Task.FromResult(new List<InventoryItem>());

        public Task<InventoryItem?> FindByNameAsync(string farmId, string name, CancellationToken ct = default) =>
            Task.FromResult<InventoryItem?>(null);

        public Task<InventoryItem> AddAsync(InventoryItem item, CancellationToken ct = default)
        {
            Items.Add(item);
            return Task.FromResult(item);
        }

        public Task UpdateAsync(InventoryItem item, CancellationToken ct = default)
        {
            UpdateCallCount++;
            return Task.CompletedTask;
        }

        public Task AddMovementAsync(StockMovement movement, CancellationToken ct = default)
        {
            AddMovementCallCount++;
            Movements.Add(movement);
            return Task.CompletedTask;
        }

        public Task<List<StockMovement>> GetMovementsAsync(string inventoryItemId, int limit = 50, CancellationToken ct = default) =>
            Task.FromResult(Movements.Where(m => m.InventoryItemId == inventoryItemId).ToList());
    }

    private class FakeStockAdjustmentRequestRepository : IStockAdjustmentRequestRepository
    {
        public List<StockAdjustmentRequest> Requests = [];
        public int UpdateCallCount;
        public int AddCallCount;

        public Task<StockAdjustmentRequest?> GetByIdAsync(string id, CancellationToken ct = default) =>
            Task.FromResult(Requests.FirstOrDefault(r => r.Id == id));

        public Task<List<StockAdjustmentRequest>> GetByFarmIdAsync(string farmId, StockAdjustmentStatus? status = null, CancellationToken ct = default) =>
            Task.FromResult(Requests.Where(r => r.FarmId == farmId && (status == null || r.Status == status)).ToList());

        public Task<StockAdjustmentRequest> AddAsync(StockAdjustmentRequest request, CancellationToken ct = default)
        {
            AddCallCount++;
            Requests.Add(request);
            return Task.FromResult(request);
        }

        public Task UpdateAsync(StockAdjustmentRequest request, CancellationToken ct = default)
        {
            UpdateCallCount++;
            return Task.CompletedTask;
        }
    }

    // Constructor-settable Role so the same fake shape drives both the "admin succeeds" and
    // "member is blocked" branches — defaults to Admin so tests that don't care about the gate
    // (e.g. the plain submit-a-request tests) don't have to opt in explicitly.
    private class FakeCurrentUserAccessor : ICurrentUserAccessor
    {
        public FakeCurrentUserAccessor(string userId = "user-1", UserRole role = UserRole.Admin)
        {
            UserId = userId;
            Role = role;
        }

        public string? UserId { get; }
        public string? CompanyId => null;
        public UserRole Role { get; }
    }

    private static InventoryItem CreateItem(string farmId = "farm-1", decimal stock = 100) =>
        InventoryItem.Create(farmId, "Glyphosate 360", "L", "cat_chemical", stock, 30);

    // ── AdjustInventoryStockCommand (submit) ──────────────────────────────────────────────────

    [Fact]
    public async Task Adjust_ValidRequest_CreatesPendingRequest_DoesNotChangeStock()
    {
        var item = CreateItem(stock: 100);
        var inventoryRepo = new FakeInventoryRepository { Items = [item] };
        var requestRepo = new FakeStockAdjustmentRequestRepository();
        var handler = new AdjustInventoryStockCommandHandler(inventoryRepo, requestRepo, new FakeCurrentUserAccessor());

        var result = await handler.Handle(new AdjustInventoryStockCommand("farm-1", item.Id, 85, "Physical recount"), CancellationToken.None);

        Assert.Equal("Pending", result.Status);
        Assert.Equal(85m, result.RequestedQuantity);
        Assert.Equal(100m, item.CurrentStock); // unchanged
        Assert.Equal(0, inventoryRepo.UpdateCallCount);
        Assert.Equal(0, inventoryRepo.AddMovementCallCount);
        Assert.Equal(1, requestRepo.AddCallCount);
    }

    [Fact]
    public async Task Adjust_BlankNotes_ThrowsDomainException()
    {
        var item = CreateItem();
        var inventoryRepo = new FakeInventoryRepository { Items = [item] };
        var handler = new AdjustInventoryStockCommandHandler(inventoryRepo, new FakeStockAdjustmentRequestRepository(), new FakeCurrentUserAccessor());

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new AdjustInventoryStockCommand("farm-1", item.Id, 85, "   "), CancellationToken.None));
    }

    [Fact]
    public async Task Adjust_ItemFromDifferentFarm_ThrowsDomainException()
    {
        var item = CreateItem(farmId: "farm-2");
        var inventoryRepo = new FakeInventoryRepository { Items = [item] };
        var handler = new AdjustInventoryStockCommandHandler(inventoryRepo, new FakeStockAdjustmentRequestRepository(), new FakeCurrentUserAccessor());

        // Route says farm-1, item actually belongs to farm-2 — IDOR guard.
        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new AdjustInventoryStockCommand("farm-1", item.Id, 85, "Recount"), CancellationToken.None));
    }

    [Fact]
    public async Task Adjust_ItemNotFound_ThrowsDomainException()
    {
        var inventoryRepo = new FakeInventoryRepository();
        var handler = new AdjustInventoryStockCommandHandler(inventoryRepo, new FakeStockAdjustmentRequestRepository(), new FakeCurrentUserAccessor());

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new AdjustInventoryStockCommand("farm-1", "missing-item", 85, "Recount"), CancellationToken.None));
    }

    // ── ApproveStockAdjustmentCommand ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Approve_AdminRole_AppliesStockAndMarksApproved()
    {
        var item = CreateItem(stock: 100);
        var request = StockAdjustmentRequest.Create("farm-1", item.Id, 85, "Physical recount", "requester-1");
        var inventoryRepo = new FakeInventoryRepository { Items = [item] };
        var requestRepo = new FakeStockAdjustmentRequestRepository { Requests = [request] };
        var handler = new ApproveStockAdjustmentCommandHandler(inventoryRepo, requestRepo, new FakeCurrentUserAccessor("admin-1", UserRole.Admin));

        var result = await handler.Handle(new ApproveStockAdjustmentCommand("farm-1", request.Id, "Confirmed by count"), CancellationToken.None);

        Assert.Equal(85m, item.CurrentStock);
        Assert.Equal("Approved", result.Status);
        Assert.Equal(StockAdjustmentStatus.Approved, request.Status);
        Assert.Equal("admin-1", request.ReviewedByUserId);
        Assert.Equal(1, inventoryRepo.UpdateCallCount);
        Assert.Equal(1, inventoryRepo.AddMovementCallCount);
        // Delta must be computed against the PRE-adjustment stock (100 -> 85 = -15), not zero.
        Assert.Equal(-15m, inventoryRepo.Movements.Single().Quantity);
        Assert.Equal(1, requestRepo.UpdateCallCount);
    }

    [Fact]
    public async Task Approve_MemberRole_ThrowsForbiddenException_NoSideEffects()
    {
        var item = CreateItem(stock: 100);
        var request = StockAdjustmentRequest.Create("farm-1", item.Id, 85, "Physical recount", "requester-1");
        var inventoryRepo = new FakeInventoryRepository { Items = [item] };
        var requestRepo = new FakeStockAdjustmentRequestRepository { Requests = [request] };
        var handler = new ApproveStockAdjustmentCommandHandler(inventoryRepo, requestRepo, new FakeCurrentUserAccessor("member-1", UserRole.Member));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new ApproveStockAdjustmentCommand("farm-1", request.Id, null), CancellationToken.None));

        Assert.Equal(100m, item.CurrentStock);
        Assert.Equal(StockAdjustmentStatus.Pending, request.Status);
        Assert.Equal(0, inventoryRepo.UpdateCallCount);
        Assert.Equal(0, inventoryRepo.AddMovementCallCount);
        Assert.Equal(0, requestRepo.UpdateCallCount);
    }

    [Fact]
    public async Task Approve_AlreadyReviewedRequest_ThrowsDomainException_EvenForAdmin()
    {
        var item = CreateItem(stock: 100);
        var request = StockAdjustmentRequest.Create("farm-1", item.Id, 85, "Physical recount", "requester-1");
        request.Approve("admin-1");
        var inventoryRepo = new FakeInventoryRepository { Items = [item] };
        var requestRepo = new FakeStockAdjustmentRequestRepository { Requests = [request] };
        var handler = new ApproveStockAdjustmentCommandHandler(inventoryRepo, requestRepo, new FakeCurrentUserAccessor("admin-2", UserRole.Admin));

        // Role check passes (admin), but the Pending guard still fires — proves the ordering.
        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new ApproveStockAdjustmentCommand("farm-1", request.Id, null), CancellationToken.None));

        // Must not double-apply the adjustment: the Pending check has to run BEFORE any item
        // mutation, or re-approving an already-Approved request would silently apply the stock
        // change a second time.
        Assert.Equal(0, inventoryRepo.UpdateCallCount);
        Assert.Equal(0, inventoryRepo.AddMovementCallCount);
    }

    [Fact]
    public async Task Approve_RequestFromDifferentFarm_ThrowsDomainException()
    {
        var item = CreateItem(stock: 100);
        var request = StockAdjustmentRequest.Create("farm-2", item.Id, 85, "Physical recount", "requester-1");
        var inventoryRepo = new FakeInventoryRepository { Items = [item] };
        var requestRepo = new FakeStockAdjustmentRequestRepository { Requests = [request] };
        var handler = new ApproveStockAdjustmentCommandHandler(inventoryRepo, requestRepo, new FakeCurrentUserAccessor("admin-1", UserRole.Admin));

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new ApproveStockAdjustmentCommand("farm-1", request.Id, null), CancellationToken.None));
    }

    // ── RejectStockAdjustmentCommand ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Reject_AdminRole_MarksRejected_NoStockOrMovementSideEffects()
    {
        var item = CreateItem(stock: 100);
        var request = StockAdjustmentRequest.Create("farm-1", item.Id, 85, "Physical recount", "requester-1");
        var inventoryRepo = new FakeInventoryRepository { Items = [item] };
        var requestRepo = new FakeStockAdjustmentRequestRepository { Requests = [request] };
        var handler = new RejectStockAdjustmentCommandHandler(inventoryRepo, requestRepo, new FakeCurrentUserAccessor("admin-1", UserRole.Admin));

        var result = await handler.Handle(new RejectStockAdjustmentCommand("farm-1", request.Id, "Numbers don't add up"), CancellationToken.None);

        Assert.Equal(100m, item.CurrentStock); // untouched
        Assert.Equal("Rejected", result.Status);
        Assert.Equal(StockAdjustmentStatus.Rejected, request.Status);
        Assert.Equal(0, inventoryRepo.UpdateCallCount);
        Assert.Equal(0, inventoryRepo.AddMovementCallCount);
        Assert.Equal(1, requestRepo.UpdateCallCount);
    }

    [Fact]
    public async Task Reject_MemberRole_ThrowsForbiddenException()
    {
        var item = CreateItem(stock: 100);
        var request = StockAdjustmentRequest.Create("farm-1", item.Id, 85, "Physical recount", "requester-1");
        var inventoryRepo = new FakeInventoryRepository { Items = [item] };
        var requestRepo = new FakeStockAdjustmentRequestRepository { Requests = [request] };
        var handler = new RejectStockAdjustmentCommandHandler(inventoryRepo, requestRepo, new FakeCurrentUserAccessor("member-1", UserRole.Member));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new RejectStockAdjustmentCommand("farm-1", request.Id, null), CancellationToken.None));

        Assert.Equal(StockAdjustmentStatus.Pending, request.Status);
    }

    [Fact]
    public async Task Reject_AlreadyReviewedRequest_ThrowsDomainException_EvenForAdmin()
    {
        var item = CreateItem(stock: 100);
        var request = StockAdjustmentRequest.Create("farm-1", item.Id, 85, "Physical recount", "requester-1");
        request.Reject("admin-1");
        var inventoryRepo = new FakeInventoryRepository { Items = [item] };
        var requestRepo = new FakeStockAdjustmentRequestRepository { Requests = [request] };
        var handler = new RejectStockAdjustmentCommandHandler(inventoryRepo, requestRepo, new FakeCurrentUserAccessor("admin-2", UserRole.Admin));

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new RejectStockAdjustmentCommand("farm-1", request.Id, null), CancellationToken.None));
    }

    // ── GetStockAdjustmentRequestsQuery ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetQueue_NoStatusFilter_ReturnsAllStatusesForFarm()
    {
        var item = CreateItem();
        var pending = StockAdjustmentRequest.Create("farm-1", item.Id, 85, "Recount", "user-1");
        var approved = StockAdjustmentRequest.Create("farm-1", item.Id, 90, "Recount 2", "user-1");
        approved.Approve("admin-1");
        var otherFarm = StockAdjustmentRequest.Create("farm-2", item.Id, 10, "Recount 3", "user-1");
        var inventoryRepo = new FakeInventoryRepository { Items = [item] };
        var requestRepo = new FakeStockAdjustmentRequestRepository { Requests = [pending, approved, otherFarm] };
        var handler = new GetStockAdjustmentRequestsQueryHandler(requestRepo, inventoryRepo);

        var result = await handler.Handle(new GetStockAdjustmentRequestsQuery("farm-1", null), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Id == pending.Id && r.InventoryItemName == "Glyphosate 360");
        Assert.Contains(result, r => r.Id == approved.Id);
    }

    [Fact]
    public async Task GetQueue_PendingFilter_ReturnsOnlyPending()
    {
        var item = CreateItem();
        var pending = StockAdjustmentRequest.Create("farm-1", item.Id, 85, "Recount", "user-1");
        var approved = StockAdjustmentRequest.Create("farm-1", item.Id, 90, "Recount 2", "user-1");
        approved.Approve("admin-1");
        var inventoryRepo = new FakeInventoryRepository { Items = [item] };
        var requestRepo = new FakeStockAdjustmentRequestRepository { Requests = [pending, approved] };
        var handler = new GetStockAdjustmentRequestsQueryHandler(requestRepo, inventoryRepo);

        var result = await handler.Handle(new GetStockAdjustmentRequestsQuery("farm-1", StockAdjustmentStatus.Pending), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(pending.Id, result[0].Id);
        Assert.Equal("Pending", result[0].Status);
    }
}
