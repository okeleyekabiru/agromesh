using AgroMesh.Application.Commands.Order;
using AgroMesh.Application.Commands.Supplier;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Queries.Order;
using AgroMesh.Application.Queries.Supplier;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;

namespace AgroMesh.Tests;

/// <summary>
/// Handler tests for Phase 4 (Orders/Suppliers/Invoices) — fakes mirror
/// StockAdjustmentApprovalTests.cs's FakeInventoryRepository shape (no mocking library, no
/// in-memory EF). Supplier-only cases live here first; Order cases are appended as the plan's
/// later steps build out CreateOrder/PlaceOrder/ReceiveOrder/GetOrders.
/// </summary>
public class SupplierAndOrderHandlerTests
{
    private class FakeSupplierRepository : ISupplierRepository
    {
        public List<Supplier> Suppliers = [];
        public int AddCallCount;
        public int GetByIdCallCount;
        public int GetByFarmIdCallCount;

        public Task<Supplier?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            GetByIdCallCount++;
            return Task.FromResult(Suppliers.FirstOrDefault(s => s.Id == id));
        }

        public Task<List<Supplier>> GetByFarmIdAsync(string farmId, CancellationToken ct = default)
        {
            GetByFarmIdCallCount++;
            return Task.FromResult(Suppliers.Where(s => s.FarmId == farmId).ToList());
        }

        public Task<Supplier> AddAsync(Supplier supplier, CancellationToken ct = default)
        {
            AddCallCount++;
            Suppliers.Add(supplier);
            return Task.FromResult(supplier);
        }
    }

    // Local re-declaration of the fake shape StockAdjustmentApprovalTests.cs's FakeInventoryRepository
    // uses (no shared test-helpers file exists in this repo — each test file declares its own fakes).
    private class FakeInventoryRepository : IInventoryRepository
    {
        public List<InventoryItem> Items = [];
        public List<StockMovement> Movements = [];
        public int UpdateCallCount;
        public int AddMovementCallCount;
        public int GetByIdCallCount;
        public int GetByFarmIdCallCount;

        public Task<InventoryItem?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            GetByIdCallCount++;
            return Task.FromResult(Items.FirstOrDefault(i => i.Id == id));
        }

        public Task<List<InventoryItem>> GetByFarmIdAsync(string farmId, CancellationToken ct = default)
        {
            GetByFarmIdCallCount++;
            return Task.FromResult(Items.Where(i => i.FarmId == farmId).ToList());
        }

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

        public Task UpdateMovementAsync(StockMovement movement, CancellationToken ct = default) => Task.CompletedTask;

        public Task<List<StockMovement>> GetOutstandingPicksAsync(string farmId, string? inventoryItemId = null, string? blockId = null, CancellationToken ct = default) =>
            Task.FromResult(new List<StockMovement>());

        public Task<List<StockMovement>> GetMovementsAsync(string inventoryItemId, int limit = 50, CancellationToken ct = default) =>
            Task.FromResult(Movements.Where(m => m.InventoryItemId == inventoryItemId).ToList());
    }

    private class FakeOrderRepository : IOrderRepository
    {
        public List<Domain.Entities.Order> Orders = [];
        public int AddCallCount;
        public int UpdateCallCount;

        public Task<Domain.Entities.Order?> GetByIdAsync(string id, CancellationToken ct = default) =>
            Task.FromResult(Orders.FirstOrDefault(o => o.Id == id));

        public Task<Domain.Entities.Order?> GetByNumberAsync(string farmId, int number, CancellationToken ct = default) =>
            Task.FromResult(Orders.FirstOrDefault(o => o.FarmId == farmId && o.Number == number));

        public Task<int> GetNextNumberAsync(string farmId, CancellationToken ct = default) =>
            Task.FromResult(Math.Max(Orders.Where(o => o.FarmId == farmId).Select(o => o.Number).DefaultIfEmpty(0).Max(), 1000) + 1);

        public Task<List<Domain.Entities.Order>> GetByFarmIdAsync(string farmId, OrderStatus? status = null, CancellationToken ct = default) =>
            Task.FromResult(Orders.Where(o => o.FarmId == farmId && (status == null || o.Status == status)).ToList());

        public Task<Domain.Entities.Order> AddAsync(Domain.Entities.Order order, CancellationToken ct = default)
        {
            AddCallCount++;
            Orders.Add(order);
            return Task.FromResult(order);
        }

        public Task UpdateAsync(Domain.Entities.Order order, CancellationToken ct = default)
        {
            UpdateCallCount++;
            return Task.CompletedTask;
        }
    }

    private static InventoryItem CreateItem(string farmId = "farm-1", string name = "Glyphosate 360", decimal stock = 100) =>
        InventoryItem.Create(farmId, name, "L", "cat_chemical", stock, 30);

    // ── CreateSupplierCommand ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateSupplier_Valid_PersistsAndReturnsDto()
    {
        var repo = new FakeSupplierRepository();
        var handler = new CreateSupplierCommandHandler(repo);

        var result = await handler.Handle(
            new CreateSupplierCommand("farm-1", new CreateSupplierRequest("Saro Agro Sciences", "0801", "sales@saro.com")),
            CancellationToken.None);

        Assert.Equal("farm-1", result.FarmId);
        Assert.Equal("Saro Agro Sciences", result.Name);
        Assert.Equal("0801", result.Phone);
        Assert.Equal("sales@saro.com", result.Email);
        Assert.Equal(1, repo.AddCallCount);
    }

    [Fact]
    public async Task CreateSupplier_BlankName_ThrowsDomainException()
    {
        var repo = new FakeSupplierRepository();
        var handler = new CreateSupplierCommandHandler(repo);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new CreateSupplierCommand("farm-1", new CreateSupplierRequest("  ", null, null)), CancellationToken.None));

        Assert.Equal(0, repo.AddCallCount);
    }

    // ── GetSuppliersQuery ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSuppliers_ReturnsOnlyRequestedFarmsSuppliers()
    {
        var repo = new FakeSupplierRepository
        {
            Suppliers =
            [
                Domain.Entities.Supplier.Create("farm-1", "Saro Agro"),
                Domain.Entities.Supplier.Create("farm-2", "Candel Co")
            ]
        };
        var handler = new GetSuppliersQueryHandler(repo);

        var result = await handler.Handle(new GetSuppliersQuery("farm-1"), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Saro Agro", result[0].Name);
    }

    // ── CreateOrderCommand ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateOrder_ValidLineItems_PersistsDraftOrderWithResolvedNames()
    {
        var item = CreateItem();
        var supplier = Domain.Entities.Supplier.Create("farm-1", "Saro Agro");
        var inventoryRepo = new FakeInventoryRepository { Items = [item] };
        var supplierRepo = new FakeSupplierRepository { Suppliers = [supplier] };
        var orderRepo = new FakeOrderRepository();
        var handler = new CreateOrderCommandHandler(orderRepo, supplierRepo, inventoryRepo);

        var request = new CreateOrderRequest(supplier.Id, [new CreateOrderLineItemRequest(item.Id, 10, 500)]);
        var result = await handler.Handle(new CreateOrderCommand("farm-1", request), CancellationToken.None);

        Assert.Equal("Draft", result.Status);
        Assert.Equal("Saro Agro", result.SupplierName);
        Assert.Single(result.LineItems);
        Assert.Equal("Glyphosate 360", result.LineItems[0].InventoryItemName);
        Assert.Equal(5000m, result.Total);
        Assert.Equal(1, orderRepo.AddCallCount);
    }

    [Fact]
    public async Task CreateOrder_UnknownInventoryItem_ThrowsDomainException()
    {
        var supplier = Domain.Entities.Supplier.Create("farm-1", "Saro Agro");
        var inventoryRepo = new FakeInventoryRepository();
        var supplierRepo = new FakeSupplierRepository { Suppliers = [supplier] };
        var orderRepo = new FakeOrderRepository();
        var handler = new CreateOrderCommandHandler(orderRepo, supplierRepo, inventoryRepo);

        var request = new CreateOrderRequest(supplier.Id, [new CreateOrderLineItemRequest("missing-item", 10, 500)]);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new CreateOrderCommand("farm-1", request), CancellationToken.None));
        Assert.Equal(0, orderRepo.AddCallCount);
    }

    [Fact]
    public async Task CreateOrder_InventoryItemFromDifferentFarm_ThrowsDomainException()
    {
        var item = CreateItem(farmId: "farm-2");
        var supplier = Domain.Entities.Supplier.Create("farm-1", "Saro Agro");
        var inventoryRepo = new FakeInventoryRepository { Items = [item] };
        var supplierRepo = new FakeSupplierRepository { Suppliers = [supplier] };
        var orderRepo = new FakeOrderRepository();
        var handler = new CreateOrderCommandHandler(orderRepo, supplierRepo, inventoryRepo);

        var request = new CreateOrderRequest(supplier.Id, [new CreateOrderLineItemRequest(item.Id, 10, 500)]);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new CreateOrderCommand("farm-1", request), CancellationToken.None));
        Assert.Equal(0, orderRepo.AddCallCount);
    }

    [Fact]
    public async Task CreateOrder_UnknownSupplier_ThrowsDomainException()
    {
        var item = CreateItem();
        var inventoryRepo = new FakeInventoryRepository { Items = [item] };
        var supplierRepo = new FakeSupplierRepository();
        var orderRepo = new FakeOrderRepository();
        var handler = new CreateOrderCommandHandler(orderRepo, supplierRepo, inventoryRepo);

        var request = new CreateOrderRequest("missing-supplier", [new CreateOrderLineItemRequest(item.Id, 10, 500)]);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new CreateOrderCommand("farm-1", request), CancellationToken.None));
        Assert.Equal(0, orderRepo.AddCallCount);
    }

    [Fact]
    public async Task CreateOrder_SupplierFromDifferentFarm_ThrowsDomainException()
    {
        var item = CreateItem();
        var supplier = Domain.Entities.Supplier.Create("farm-2", "Saro Agro");
        var inventoryRepo = new FakeInventoryRepository { Items = [item] };
        var supplierRepo = new FakeSupplierRepository { Suppliers = [supplier] };
        var orderRepo = new FakeOrderRepository();
        var handler = new CreateOrderCommandHandler(orderRepo, supplierRepo, inventoryRepo);

        var request = new CreateOrderRequest(supplier.Id, [new CreateOrderLineItemRequest(item.Id, 10, 500)]);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new CreateOrderCommand("farm-1", request), CancellationToken.None));
        Assert.Equal(0, orderRepo.AddCallCount);
    }

    // ── PlaceOrderCommand ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PlaceOrder_FromDraft_UpdatesStatus()
    {
        var item = CreateItem();
        var supplier = Domain.Entities.Supplier.Create("farm-1", "Saro Agro");
        var order = Domain.Entities.Order.Create("farm-1", supplier.Id, [new AgroMesh.Domain.ValueObjects.OrderLineItem(item.Id, 10, 500)]);
        var inventoryRepo = new FakeInventoryRepository { Items = [item] };
        var supplierRepo = new FakeSupplierRepository { Suppliers = [supplier] };
        var orderRepo = new FakeOrderRepository { Orders = [order] };
        var handler = new PlaceOrderCommandHandler(orderRepo, supplierRepo, inventoryRepo);

        var result = await handler.Handle(new PlaceOrderCommand("farm-1", order.Id), CancellationToken.None);

        Assert.Equal("Placed", result.Status);
        Assert.Equal(OrderStatus.Placed, order.Status);
        Assert.Equal(1, orderRepo.UpdateCallCount);
    }

    [Fact]
    public async Task PlaceOrder_FromPlaced_ThrowsDomainException_NoUpdateCall()
    {
        var item = CreateItem();
        var supplier = Domain.Entities.Supplier.Create("farm-1", "Saro Agro");
        var order = Domain.Entities.Order.Create("farm-1", supplier.Id, [new AgroMesh.Domain.ValueObjects.OrderLineItem(item.Id, 10, 500)]);
        order.Place();
        var inventoryRepo = new FakeInventoryRepository { Items = [item] };
        var supplierRepo = new FakeSupplierRepository { Suppliers = [supplier] };
        var orderRepo = new FakeOrderRepository { Orders = [order] };
        var handler = new PlaceOrderCommandHandler(orderRepo, supplierRepo, inventoryRepo);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new PlaceOrderCommand("farm-1", order.Id), CancellationToken.None));
        Assert.Equal(0, orderRepo.UpdateCallCount);
    }

    [Fact]
    public async Task PlaceOrder_DifferentFarm_ThrowsDomainException()
    {
        var item = CreateItem();
        var supplier = Domain.Entities.Supplier.Create("farm-2", "Saro Agro");
        var order = Domain.Entities.Order.Create("farm-2", supplier.Id, [new AgroMesh.Domain.ValueObjects.OrderLineItem(item.Id, 10, 500)]);
        var inventoryRepo = new FakeInventoryRepository { Items = [item] };
        var supplierRepo = new FakeSupplierRepository { Suppliers = [supplier] };
        var orderRepo = new FakeOrderRepository { Orders = [order] };
        var handler = new PlaceOrderCommandHandler(orderRepo, supplierRepo, inventoryRepo);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new PlaceOrderCommand("farm-1", order.Id), CancellationToken.None));
        Assert.Equal(0, orderRepo.UpdateCallCount);
    }

    // ── ReceiveOrderCommand ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReceiveOrder_FromPlaced_IncreasesStockAndRecordsInMovementsPerLine()
    {
        var item1 = CreateItem(name: "Glyphosate 360", stock: 100);
        var item2 = CreateItem(name: "NPK 15-15-15", stock: 50);
        var supplier = Domain.Entities.Supplier.Create("farm-1", "Saro Agro");
        var order = Domain.Entities.Order.Create("farm-1", supplier.Id,
        [
            new AgroMesh.Domain.ValueObjects.OrderLineItem(item1.Id, 10, 500),
            new AgroMesh.Domain.ValueObjects.OrderLineItem(item2.Id, 20, 1200)
        ]);
        order.Place();
        var inventoryRepo = new FakeInventoryRepository { Items = [item1, item2] };
        var supplierRepo = new FakeSupplierRepository { Suppliers = [supplier] };
        var orderRepo = new FakeOrderRepository { Orders = [order] };
        var handler = new ReceiveOrderCommandHandler(orderRepo, supplierRepo, inventoryRepo);

        var result = await handler.Handle(new ReceiveOrderCommand("farm-1", order.Id), CancellationToken.None);

        Assert.Equal("Received", result.Status);
        Assert.Equal(OrderStatus.Received, order.Status);
        Assert.Equal(110m, item1.CurrentStock); // 100 + 10
        Assert.Equal(70m, item2.CurrentStock);  // 50 + 20
        Assert.Equal(2, inventoryRepo.UpdateCallCount);
        Assert.Equal(2, inventoryRepo.AddMovementCallCount);
        Assert.All(inventoryRepo.Movements, m => Assert.Equal(order.Id, m.OrderId));
        Assert.All(inventoryRepo.Movements, m => Assert.Equal(StockMovementType.In, m.Type));
        Assert.Equal(1, orderRepo.UpdateCallCount);
    }

    [Fact]
    public async Task ReceiveOrder_SetsTheStockCostFromTheOrdersUnitPrice_AsAWeightedAverage()
    {
        var item = CreateItem(stock: 100);
        item.SetUnitCost(400m);                                  // 100 on hand at 400 each
        var supplier = Domain.Entities.Supplier.Create("farm-1", "Saro Agro");
        var order = Domain.Entities.Order.Create("farm-1", supplier.Id, [new AgroMesh.Domain.ValueObjects.OrderLineItem(item.Id, 100, 600)]);
        order.Place();
        var handler = new ReceiveOrderCommandHandler(
            new FakeOrderRepository { Orders = [order] }, new FakeSupplierRepository { Suppliers = [supplier] },
            new FakeInventoryRepository { Items = [item] });

        await handler.Handle(new ReceiveOrderCommand("farm-1", order.Id), CancellationToken.None);

        Assert.Equal(200m, item.CurrentStock);
        Assert.Equal(500m, item.UnitCost);                       // (100 x 400 + 100 x 600) / 200
    }

    [Fact]
    public async Task ReceiveOrder_PricesStockThatHadNoCost()
    {
        var item = CreateItem(stock: 0);
        var supplier = Domain.Entities.Supplier.Create("farm-1", "Saro Agro");
        var order = Domain.Entities.Order.Create("farm-1", supplier.Id, [new AgroMesh.Domain.ValueObjects.OrderLineItem(item.Id, 10, 560)]);
        order.Place();
        var handler = new ReceiveOrderCommandHandler(
            new FakeOrderRepository { Orders = [order] }, new FakeSupplierRepository { Suppliers = [supplier] },
            new FakeInventoryRepository { Items = [item] });

        await handler.Handle(new ReceiveOrderCommand("farm-1", order.Id), CancellationToken.None);

        Assert.Equal(560m, item.UnitCost);
    }

    [Fact]
    public async Task ReceiveOrder_FromDraft_ThrowsDomainException_NoStockOrMovementSideEffects()
    {
        var item = CreateItem(stock: 100);
        var supplier = Domain.Entities.Supplier.Create("farm-1", "Saro Agro");
        var order = Domain.Entities.Order.Create("farm-1", supplier.Id, [new AgroMesh.Domain.ValueObjects.OrderLineItem(item.Id, 10, 500)]);
        var inventoryRepo = new FakeInventoryRepository { Items = [item] };
        var supplierRepo = new FakeSupplierRepository { Suppliers = [supplier] };
        var orderRepo = new FakeOrderRepository { Orders = [order] };
        var handler = new ReceiveOrderCommandHandler(orderRepo, supplierRepo, inventoryRepo);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new ReceiveOrderCommand("farm-1", order.Id), CancellationToken.None));

        Assert.Equal(100m, item.CurrentStock);
        Assert.Equal(0, inventoryRepo.UpdateCallCount);
        Assert.Equal(0, inventoryRepo.AddMovementCallCount);
        Assert.Equal(0, orderRepo.UpdateCallCount);
    }

    [Fact]
    public async Task ReceiveOrder_AlreadyReceived_ThrowsDomainException_NoSideEffects()
    {
        var item = CreateItem(stock: 100);
        var supplier = Domain.Entities.Supplier.Create("farm-1", "Saro Agro");
        var order = Domain.Entities.Order.Create("farm-1", supplier.Id, [new AgroMesh.Domain.ValueObjects.OrderLineItem(item.Id, 10, 500)]);
        order.Place();
        order.Receive();
        var inventoryRepo = new FakeInventoryRepository { Items = [item] };
        var supplierRepo = new FakeSupplierRepository { Suppliers = [supplier] };
        var orderRepo = new FakeOrderRepository { Orders = [order] };
        var handler = new ReceiveOrderCommandHandler(orderRepo, supplierRepo, inventoryRepo);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new ReceiveOrderCommand("farm-1", order.Id), CancellationToken.None));

        Assert.Equal(100m, item.CurrentStock);
        Assert.Equal(0, inventoryRepo.UpdateCallCount);
        Assert.Equal(0, inventoryRepo.AddMovementCallCount);
        Assert.Equal(0, orderRepo.UpdateCallCount);
    }

    [Fact]
    public async Task ReceiveOrder_LineItemNoLongerResolvable_ThrowsDomainException_NoPartialStockApplied()
    {
        // Two line items — the second references an item that has since been removed from the
        // repo. Must fail atomically: the first line's stock must NOT have been applied either.
        var item1 = CreateItem(name: "Glyphosate 360", stock: 100);
        var supplier = Domain.Entities.Supplier.Create("farm-1", "Saro Agro");
        var order = Domain.Entities.Order.Create("farm-1", supplier.Id,
        [
            new AgroMesh.Domain.ValueObjects.OrderLineItem(item1.Id, 10, 500),
            new AgroMesh.Domain.ValueObjects.OrderLineItem("missing-item", 5, 200)
        ]);
        order.Place();
        var inventoryRepo = new FakeInventoryRepository { Items = [item1] }; // missing-item not present
        var supplierRepo = new FakeSupplierRepository { Suppliers = [supplier] };
        var orderRepo = new FakeOrderRepository { Orders = [order] };
        var handler = new ReceiveOrderCommandHandler(orderRepo, supplierRepo, inventoryRepo);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new ReceiveOrderCommand("farm-1", order.Id), CancellationToken.None));

        Assert.Equal(100m, item1.CurrentStock); // unchanged — no partial application
        Assert.Equal(0, inventoryRepo.UpdateCallCount);
        Assert.Equal(0, inventoryRepo.AddMovementCallCount);
        Assert.Equal(OrderStatus.Placed, order.Status);
    }

    // ── GetOrdersQuery / GetOrderByIdQuery ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrders_NoFilter_ReturnsAllStatusesForFarm()
    {
        var item = CreateItem();
        var supplier = Domain.Entities.Supplier.Create("farm-1", "Saro Agro");
        var draft = Domain.Entities.Order.Create("farm-1", supplier.Id, [new AgroMesh.Domain.ValueObjects.OrderLineItem(item.Id, 10, 500)]);
        var placed = Domain.Entities.Order.Create("farm-1", supplier.Id, [new AgroMesh.Domain.ValueObjects.OrderLineItem(item.Id, 5, 200)]);
        placed.Place();
        var otherFarm = Domain.Entities.Order.Create("farm-2", supplier.Id, [new AgroMesh.Domain.ValueObjects.OrderLineItem(item.Id, 1, 100)]);
        var inventoryRepo = new FakeInventoryRepository { Items = [item] };
        var supplierRepo = new FakeSupplierRepository { Suppliers = [supplier] };
        var orderRepo = new FakeOrderRepository { Orders = [draft, placed, otherFarm] };
        var handler = new GetOrdersQueryHandler(orderRepo, supplierRepo, inventoryRepo);

        var result = await handler.Handle(new GetOrdersQuery("farm-1", null), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, o => o.Id == draft.Id);
        Assert.Contains(result, o => o.Id == placed.Id);
    }

    [Fact]
    public async Task GetOrders_StatusFilter_ReturnsOnlyMatching()
    {
        var item = CreateItem();
        var supplier = Domain.Entities.Supplier.Create("farm-1", "Saro Agro");
        var draft = Domain.Entities.Order.Create("farm-1", supplier.Id, [new AgroMesh.Domain.ValueObjects.OrderLineItem(item.Id, 10, 500)]);
        var placed = Domain.Entities.Order.Create("farm-1", supplier.Id, [new AgroMesh.Domain.ValueObjects.OrderLineItem(item.Id, 5, 200)]);
        placed.Place();
        var inventoryRepo = new FakeInventoryRepository { Items = [item] };
        var supplierRepo = new FakeSupplierRepository { Suppliers = [supplier] };
        var orderRepo = new FakeOrderRepository { Orders = [draft, placed] };
        var handler = new GetOrdersQueryHandler(orderRepo, supplierRepo, inventoryRepo);

        var result = await handler.Handle(new GetOrdersQuery("farm-1", OrderStatus.Placed), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(placed.Id, result[0].Id);
        Assert.Equal("Placed", result[0].Status);
    }

    [Fact]
    public async Task GetOrders_MultipleOrders_ResolvesSupplierAndItemsWithBulkFetchesNotPerOrder()
    {
        // Regression guard for the same N+1 avoidance GetStockAdjustmentRequestsQueryHandler already
        // establishes (one GetByFarmIdAsync per farm, joined in memory) — three orders sharing one
        // supplier and one item must not trigger three rounds of per-order lookups.
        var item = CreateItem();
        var supplier = Domain.Entities.Supplier.Create("farm-1", "Saro Agro");
        var orders = Enumerable.Range(0, 3)
            .Select(_ => Domain.Entities.Order.Create("farm-1", supplier.Id, [new AgroMesh.Domain.ValueObjects.OrderLineItem(item.Id, 1, 100)]))
            .ToList();
        var inventoryRepo = new FakeInventoryRepository { Items = [item] };
        var supplierRepo = new FakeSupplierRepository { Suppliers = [supplier] };
        var orderRepo = new FakeOrderRepository { Orders = orders };
        var handler = new GetOrdersQueryHandler(orderRepo, supplierRepo, inventoryRepo);

        var result = await handler.Handle(new GetOrdersQuery("farm-1", null), CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.Equal(0, supplierRepo.GetByIdCallCount);
        Assert.Equal(1, supplierRepo.GetByFarmIdCallCount);
        Assert.Equal(0, inventoryRepo.GetByIdCallCount);
        Assert.Equal(1, inventoryRepo.GetByFarmIdCallCount);
    }

    [Fact]
    public async Task GetOrderById_DifferentFarm_ThrowsDomainException()
    {
        var item = CreateItem(farmId: "farm-2");
        var supplier = Domain.Entities.Supplier.Create("farm-2", "Saro Agro");
        var order = Domain.Entities.Order.Create("farm-2", supplier.Id, [new AgroMesh.Domain.ValueObjects.OrderLineItem(item.Id, 10, 500)]);
        var inventoryRepo = new FakeInventoryRepository { Items = [item] };
        var supplierRepo = new FakeSupplierRepository { Suppliers = [supplier] };
        var orderRepo = new FakeOrderRepository { Orders = [order] };
        var handler = new GetOrderByIdQueryHandler(orderRepo, supplierRepo, inventoryRepo);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new GetOrderByIdQuery("farm-1", order.Id), CancellationToken.None));
    }

    [Fact]
    public async Task GetOrderById_ValidFarm_ReturnsOrderWithLineItems()
    {
        var item = CreateItem();
        var supplier = Domain.Entities.Supplier.Create("farm-1", "Saro Agro");
        var order = Domain.Entities.Order.Create("farm-1", supplier.Id, [new AgroMesh.Domain.ValueObjects.OrderLineItem(item.Id, 10, 500)]);
        var inventoryRepo = new FakeInventoryRepository { Items = [item] };
        var supplierRepo = new FakeSupplierRepository { Suppliers = [supplier] };
        var orderRepo = new FakeOrderRepository { Orders = [order] };
        var handler = new GetOrderByIdQueryHandler(orderRepo, supplierRepo, inventoryRepo);

        var result = await handler.Handle(new GetOrderByIdQuery("farm-1", order.Id), CancellationToken.None);

        Assert.Equal(order.Id, result.Id);
        Assert.Single(result.LineItems);
        Assert.Equal(5000m, result.Total);
    }

    // ── GenerateOrderInvoiceQuery ──────────────────────────────────────────────────────────────

    private class FakeOrderInvoiceService : IOrderInvoiceService
    {
        public byte[] BytesToReturn = [1, 2, 3];
        public int CallCount;
        public OrderDto? LastOrder;
        public Farm? LastFarm;

        public byte[] Generate(OrderDto order, Farm farm)
        {
            CallCount++;
            LastOrder = order;
            LastFarm = farm;
            return BytesToReturn;
        }
    }

    private class FakeFarmRepository : IFarmRepository
    {
        public List<Farm> Farms = [];

        public Task<Farm?> GetByIdAsync(string id, CancellationToken ct = default) =>
            Task.FromResult(Farms.FirstOrDefault(f => f.Id == id));

        public Task<Farm?> GetByIdWithBlocksAsync(string id, CancellationToken ct = default) =>
            Task.FromResult(Farms.FirstOrDefault(f => f.Id == id));

        public Task<List<Farm>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(Farms);

        public Task<List<Farm>> GetByOwnerIdAsync(string ownerId, CancellationToken ct = default) =>
            Task.FromResult(Farms.Where(f => f.OwnerId == ownerId).ToList());

        public Task<List<Farm>> GetByCompanyIdAsync(string companyId, CancellationToken ct = default) =>
            Task.FromResult(Farms.Where(f => f.CompanyId == companyId).ToList());

        public Task<Farm> AddAsync(Farm farm, CancellationToken ct = default)
        {
            Farms.Add(farm);
            return Task.FromResult(farm);
        }

        public Task UpdateAsync(Farm farm, CancellationToken ct = default) => Task.CompletedTask;

        public Task<bool> ExistsAsync(string id, CancellationToken ct = default) =>
            Task.FromResult(Farms.Any(f => f.Id == id));
    }

    [Fact]
    public async Task GenerateOrderInvoice_ValidOrder_CallsInvoiceServiceAndReturnsBytes()
    {
        var item = CreateItem();
        var supplier = Domain.Entities.Supplier.Create("farm-1", "Saro Agro");
        var order = Domain.Entities.Order.Create("farm-1", supplier.Id, [new AgroMesh.Domain.ValueObjects.OrderLineItem(item.Id, 10, 500)]);
        var farm = Domain.Entities.Farm.Create("Kabiru Farms", "company-1", "owner-1", id: "farm-1");
        var inventoryRepo = new FakeInventoryRepository { Items = [item] };
        var supplierRepo = new FakeSupplierRepository { Suppliers = [supplier] };
        var orderRepo = new FakeOrderRepository { Orders = [order] };
        var farmRepo = new FakeFarmRepository { Farms = [farm] };
        var invoiceService = new FakeOrderInvoiceService();
        var handler = new GenerateOrderInvoiceQueryHandler(orderRepo, supplierRepo, inventoryRepo, farmRepo, invoiceService);

        var result = await handler.Handle(new GenerateOrderInvoiceQuery("farm-1", order.Id), CancellationToken.None);

        Assert.Equal(1, invoiceService.CallCount);
        Assert.Equal(order.Id, invoiceService.LastOrder?.Id);
        Assert.Equal(supplier.Name, invoiceService.LastOrder?.SupplierName);
        Assert.Equal("farm-1", invoiceService.LastFarm?.Id);
        Assert.Equal(invoiceService.BytesToReturn, result);
    }

    [Fact]
    public async Task GenerateOrderInvoice_DifferentFarm_ThrowsDomainException()
    {
        var item = CreateItem(farmId: "farm-2");
        var supplier = Domain.Entities.Supplier.Create("farm-2", "Saro Agro");
        var order = Domain.Entities.Order.Create("farm-2", supplier.Id, [new AgroMesh.Domain.ValueObjects.OrderLineItem(item.Id, 10, 500)]);
        var farm = Domain.Entities.Farm.Create("Other Farm", "company-1", "owner-1", id: "farm-2");
        var inventoryRepo = new FakeInventoryRepository { Items = [item] };
        var supplierRepo = new FakeSupplierRepository { Suppliers = [supplier] };
        var orderRepo = new FakeOrderRepository { Orders = [order] };
        var farmRepo = new FakeFarmRepository { Farms = [farm] };
        var invoiceService = new FakeOrderInvoiceService();
        var handler = new GenerateOrderInvoiceQueryHandler(orderRepo, supplierRepo, inventoryRepo, farmRepo, invoiceService);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new GenerateOrderInvoiceQuery("farm-1", order.Id), CancellationToken.None));
        Assert.Equal(0, invoiceService.CallCount);
    }
}
