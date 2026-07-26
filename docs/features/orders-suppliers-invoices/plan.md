# Plan — Orders, Suppliers & Invoices

## Addendum — three live-testing bug fixes (see spec.md's addendum for full detail)

Mid-build, three issues found via live-testing were folded into this session: (1) an Add Item
quantity-corruption bug root-caused to a browser wheel-scroll footgun on number inputs, fixed with
`@wheel.prevent` app-wide; (2) Pick Stock reworked to auto-attribute the authenticated user
(`ICurrentUserAccessor`) instead of a manually-chosen `Worker`, plus a new required Section/Block
destination — `StockMovement` gained `PerformedByUserId`/`SectionId`/`BlockId` columns
(`AddStockMovementSectionAndPerformedBy` migration), `PickStockCommand`'s `IWorkerRepository`
dependency was replaced with `ISectionRepository` + `ICurrentUserAccessor`, and the Pick endpoint
gained `[Authorize]`; (3) the Orders create-form's `UnitPrice` field was verified already present
and working — no change needed. Also fixed, as pre-existing defects surfaced while keeping the full
suite green: `GetOrdersQueryHandler`'s N+1 (now bulk-fetches per farm, mirroring
`GetStockAdjustmentRequestsQueryHandler`), `web/e2e/fixtures.ts`'s stale `dailyRate` field name, and
~12 stale CSS-class e2e selectors in `web/e2e/app.spec.ts`'s Dashboard section from a prior redesign.

## Design decisions (called out per the brief's "your call, document it")

- **QuestPDF lives in `AgroMesh.Infrastructure`**, behind a new `IOrderInvoiceService` interface in `AgroMesh.Application.Interfaces` (mirrors `IClaudeSchedulingService`/`IWeatherService` — Application defines the contract, Infrastructure implements against the concrete external library). Keeps `AgroMesh.Application`/`AgroMesh.Tests` free of a QuestPDF package reference for the handler tests (which fake the interface); a small, real, non-DB-dependent test of the concrete `OrderInvoiceService` is added by giving `AgroMesh.Tests` a `ProjectReference` to `AgroMesh.Infrastructure` (new — no prior test in this repo needed one, but this is the first genuinely non-fake-able piece of new infrastructure: real PDF bytes out of a real library call).
- **`OrderLineItem` is a value object (`record`) stored as a `jsonb` column** on `Order`, via `HasConversion` + `System.Text.Json`, exactly mirroring `SectionConfiguration.Crops`. Not a separate EF-mapped child table — no query ever needs to filter/join on an individual line item, same reasoning `Section.Crops` already established.
- **Frontend feature flag: reuse `'inventory'`**, not a new `'orders'` flag. There is no backend `/api/farms/{farmId}/features` endpoint at all (confirmed absent — `featuresApi.getForFarm` 404s and `fetchFeatures()`'s catch block enables every known flag by default), so introducing a new flag name would require no backend work either way; reusing `'inventory'` is simplest and semantically correct (ordering exists to restock inventory) and needs zero changes to `FeatureName`/the fallback array.
- **Invoice download: fetch-as-blob, not `window.open`.** Auth here is a `Bearer` JWT attached by an axios request interceptor (`web/src/services/api.ts`), not a cookie — a plain `window.open(url)` navigation would hit the API with no `Authorization` header and get a 401. The store action calls `api.get(..., { responseType: 'blob' })` (through the existing `orderApi` wrapper) and the view creates an `<a>`-click-triggered `URL.createObjectURL` download, the standard pattern for authenticated file downloads with header-based auth.
- **Order "detail" is a modal, not a routed page.** No dynamic-route-param detail view exists anywhere in this frontend today (confirmed by grep) — every "detail" surface in this codebase (Section blocks, Inventory adjust/pick) is a modal opened from a list row, per `SectionsView.vue`/`BlockModal.vue`/`InventoryView.vue`. `OrdersView.vue` follows that: a list of orders, a "+ New Order" modal, and a "View" per-row action opening an `OrderDetailModal.vue` with status, line items, Place/Receive buttons, and Download Invoice.
- **`ReceiveOrderCommand` does not record who received it.** The brief marks this optional; there's no `ReceivedByUserId` field on `Order` and no requirement test for it. Kept out to match the stated non-goals (no payment/AP tracking) and avoid scope creep beyond the acceptance criteria.

## Steps

### 1. Domain: `OrderStatus` enum + `Supplier` entity
- **Files:** `api/AgroMesh.Domain/Enums/OrderStatus.cs` (new), `api/AgroMesh.Domain/Entities/Supplier.cs` (new, mirrors `InventoryCategory.cs`: `FarmId`, `Name`, `Phone`, `Email`, private ctor, static `Create` validating non-blank `Name`).
- **Test first:** `api/AgroMesh.Tests/SupplierTests.cs` — `Create_ValidName_SetsFields`, `Create_BlankName_ThrowsDomainException`, `Create_NoContactInfo_LeavesPhoneEmailNull`.

### 2. Domain: `OrderLineItem` value object + `Order` entity
- **Files:** `api/AgroMesh.Domain/ValueObjects/OrderLineItem.cs` (new, `record OrderLineItem(string InventoryItemId, decimal Quantity, decimal UnitPrice)`), `api/AgroMesh.Domain/Entities/Order.cs` (new: `FarmId`, `SupplierId`, `Status`, `List<OrderLineItem> LineItems`, computed `Total`, static `Create`, `Place()`, `Receive()`, mirroring `StockAdjustmentRequest`'s `EnsurePending`-style guard pattern).
- **Test first:** `api/AgroMesh.Tests/OrderTests.cs` — `Create_ValidLineItems_SetsStatusDraft`, `Create_EmptyLineItems_ThrowsDomainException`, `Create_LineItemWithZeroQuantity_ThrowsDomainException`, `Create_LineItemWithNegativeUnitPrice_ThrowsDomainException`, `Total_SumsQuantityTimesUnitPriceAcrossLines`, `Place_FromDraft_SetsPlaced`, `Place_FromPlaced_ThrowsDomainException`, `Place_FromReceived_ThrowsDomainException`, `Receive_FromPlaced_SetsReceived`, `Receive_FromDraft_ThrowsDomainException`, `Receive_FromReceived_ThrowsDomainException`.

### 3. Domain: extend `StockMovement.CreateIn` with `OrderId`
- **Files:** `api/AgroMesh.Domain/Entities/StockMovement.cs` (edit — add `OrderId` property + optional param on `CreateIn`, same shape as the existing `WorkerId` param added to `CreateOut` in Phase 2).
- **Test first:** extend `api/AgroMesh.Tests/InventoryTests.cs` or a small new assertion in `OrderTests.cs`/a dedicated `StockMovementTests.cs` (check if one exists first) — `CreateIn_WithOrderId_SetsOrderId`, `CreateIn_WithoutOrderId_LeavesOrderIdNull` (regression guard for the existing `RecordInventoryReceiptCommand` call site, which must keep compiling with the param optional/defaulted).
- **Risk:** must NOT change the existing positional-parameter call in `RecordInventoryReceiptCommandHandler` — add `orderId` as a new named/optional trailing parameter, don't reorder existing ones.

### 4. Application/Infrastructure: Supplier persistence + command/query
- **Files:**
  - `api/AgroMesh.Application/Interfaces/ISupplierRepository.cs` (new: `GetByIdAsync`, `GetByFarmIdAsync`, `AddAsync`)
  - `api/AgroMesh.Application/DTOs/SupplierDtos.cs` (new: `SupplierDto`, `CreateSupplierRequest`)
  - `api/AgroMesh.Application/Commands/Supplier/CreateSupplierCommand.cs` (new, mirrors `CreateInventoryCategoryCommand.cs`)
  - `api/AgroMesh.Application/Queries/Supplier/GetSuppliersQuery.cs` (new, mirrors `GetInventoryCategoriesQuery.cs`)
  - `api/AgroMesh.Infrastructure/Persistence/Configurations/SupplierConfiguration.cs` (new, mirrors `InventoryCategoryConfiguration.cs`)
  - `api/AgroMesh.Infrastructure/Persistence/Repositories/SupplierRepository.cs` (new, mirrors `InventoryCategoryRepository.cs`)
  - `api/AgroMesh.Infrastructure/Persistence/AgroMeshDbContext.cs` (edit: `DbSet<Supplier> Suppliers`)
  - `api/AgroMesh.Infrastructure/DependencyInjection.cs` (edit: register `ISupplierRepository`)
- **Test first:** `api/AgroMesh.Tests/SupplierAndOrderHandlerTests.cs` (new, fakes mirror `StockAdjustmentApprovalTests.cs`'s `FakeInventoryRepository` shape) — `CreateSupplier_Valid_PersistsAndReturnsDto`, `GetSuppliers_ReturnsOnlyRequestedFarmsSuppliers`.

### 5. Application/Infrastructure: Order persistence + Create/Place commands
- **Files:**
  - `api/AgroMesh.Application/Interfaces/IOrderRepository.cs` (new: `GetByIdAsync`, `GetByFarmIdAsync(farmId, status?)`, `AddAsync`, `UpdateAsync`)
  - `api/AgroMesh.Application/DTOs/OrderDtos.cs` (new: `OrderDto`, `OrderLineItemDto`, `CreateOrderRequest`, `CreateOrderLineItemRequest`)
  - `api/AgroMesh.Application/Commands/Order/CreateOrderCommand.cs` (new — resolves `Supplier` + each `InventoryItem` to build the DTO's names, throws `DomainException` if either isn't found or isn't farm-scoped correctly)
  - `api/AgroMesh.Application/Commands/Order/PlaceOrderCommand.cs` (new)
  - `api/AgroMesh.Infrastructure/Persistence/Configurations/OrderConfiguration.cs` (new — `LineItems` as `jsonb` via `HasConversion`, `HasOne`→`Supplier` FK, indexes on `FarmId`/`Status`)
  - `api/AgroMesh.Infrastructure/Persistence/Repositories/OrderRepository.cs` (new)
  - `api/AgroMesh.Infrastructure/Persistence/AgroMeshDbContext.cs` (edit: `DbSet<Order> Orders`)
  - `api/AgroMesh.Infrastructure/DependencyInjection.cs` (edit: register `IOrderRepository`)
- **Test first (add to `SupplierAndOrderHandlerTests.cs`):** `CreateOrder_ValidLineItems_PersistsDraftOrderWithResolvedNames`, `CreateOrder_UnknownInventoryItem_ThrowsDomainException`, `CreateOrder_UnknownSupplier_ThrowsDomainException`, `PlaceOrder_FromDraft_UpdatesStatus`, `PlaceOrder_FromPlaced_ThrowsDomainException_NoUpdateCall`.

### 6. Application: `ReceiveOrderCommand` (the side-effecting one)
- **Files:** `api/AgroMesh.Application/Commands/Order/ReceiveOrderCommand.cs` (new — per line item: `_inventoryRepo.GetByIdAsync` → `item.AddStock(qty, notes)` → `_inventoryRepo.UpdateAsync` → `StockMovement.CreateIn(item.FarmId, item.Id, qty, transactionId: null, notes, orderId: order.Id)` → `_inventoryRepo.AddMovementAsync`, then `order.Receive()` → `_orderRepo.UpdateAsync`; the `order.Receive()` Placed-check runs **before** touching any line item, same "check before mutate" ordering as `ApproveStockAdjustmentCommandHandler`).
- **Test first (add to `SupplierAndOrderHandlerTests.cs`):** `ReceiveOrder_FromPlaced_IncreasesStockAndRecordsInMovementsPerLine`, `ReceiveOrder_FromDraft_ThrowsDomainException_NoStockOrMovementSideEffects`, `ReceiveOrder_AlreadyReceived_ThrowsDomainException_NoSideEffects`, `ReceiveOrder_MovementsCarryOrderId`.

### 7. Application: `GetOrdersQuery` / `GetOrderByIdQuery`
- **Files:** `api/AgroMesh.Application/Queries/Order/GetOrdersQuery.cs` (new, optional `OrderStatus?` filter), `api/AgroMesh.Application/Queries/Order/GetOrderByIdQuery.cs` (new, throws `DomainException` on cross-farm id).
- **Test first (add to `SupplierAndOrderHandlerTests.cs`):** `GetOrders_NoFilter_ReturnsAllStatusesForFarm`, `GetOrders_StatusFilter_ReturnsOnlyMatching`, `GetOrderById_DifferentFarm_ThrowsDomainException`.

### 8. Infrastructure: QuestPDF invoice service
- **Files:**
  - `api/AgroMesh.Infrastructure/AgroMesh.Infrastructure.csproj` (edit: add `QuestPDF` package)
  - `api/AgroMesh.Application/Interfaces/IOrderInvoiceService.cs` (new: `byte[] Generate(Order order, Supplier supplier, Farm farm)`)
  - `api/AgroMesh.Infrastructure/Services/OrderInvoiceService.cs` (new QuestPDF document: farm name, supplier name/contact, status, a line-items table, order total)
  - `api/AgroMesh.Infrastructure/DependencyInjection.cs` (edit: register `IOrderInvoiceService`)
  - `api/AgroMesh.API/Program.cs` (edit: `QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;` near other startup config)
  - `api/AgroMesh.Application/Queries/Order/GenerateOrderInvoiceQuery.cs` (new — resolves `Order`+`Supplier`+`Farm`, calls `IOrderInvoiceService.Generate`, returns bytes)
  - `api/AgroMesh.Tests/AgroMesh.Tests.csproj` (edit: add `ProjectReference` to `AgroMesh.Infrastructure`)
- **Test first:** `api/AgroMesh.Tests/OrderInvoiceServiceTests.cs` (new, real QuestPDF, sets `QuestPDF.Settings.License` itself in a static ctor since `Program.cs` doesn't run under xUnit) — `Generate_RealOrder_ReturnsNonEmptyPdfBytes` (asserts byte length > 0 and the first 4 bytes are the `%PDF` magic number). Add `GenerateOrderInvoiceQuery` handler test to `SupplierAndOrderHandlerTests.cs` with a fake `IOrderInvoiceService` proving it's wired correctly (farm-scoping check included).

### 9. API layer: controllers + migrations + `[Authorize]` proof
- **Files:**
  - `api/AgroMesh.API/Controllers/SuppliersController.cs` (new, `[Authorize]`, `api/farms/{farmId}/suppliers`, `GET`/`POST`)
  - `api/AgroMesh.API/Controllers/OrdersController.cs` (new, `[Authorize]`, `api/farms/{farmId}/orders`: `GET /`, `POST /`, `GET /{orderId}`, `PATCH /{orderId}/place`, `PATCH /{orderId}/receive`, `GET /{orderId}/invoice` → `File(bytes, "application/pdf", $"invoice-{orderId}.pdf")`)
  - EF migration (via `dotnet ef migrations add AddSuppliersAndOrders --project AgroMesh.Infrastructure --startup-project AgroMesh.API`): a single migration — EF batches all pending model changes together, so the `Suppliers` table, `Orders` table (with the `jsonb` `LineItems` column + FK to `Suppliers`), and the `StockMovements.OrderId` column (deferred from step 3, which only changed the domain entity/EF config, not the DB) all land in one `AddSuppliersAndOrders` migration rather than two separate ones as originally planned — apply against the local Postgres container (port 5433) as part of manual verification, per repo convention (`Program.cs` auto-migrates in Development).
- **Test first:** `api/AgroMesh.Tests/OrdersAuthorizeAttributeTests.cs` (new — plain reflection: `typeof(SuppliersController).GetCustomAttributes<AuthorizeAttribute>().Any()` is true, same for `OrdersController`, and `typeof(InventoryController)`/`InventoryCategoriesController` still have **zero** `AuthorizeAttribute`s — a regression guard proving Phase 1-3 endpoints were not retroactively gated).
- **Manual verification:** once migrations are applied, a live pass against `agromesh_api`/`agromesh_db` (docker-compose, port 5433): create supplier → create Draft order → place → receive → confirm `CurrentStock` increased via `GET /inventory` → `GET /orders/{id}/invoice` and confirm `Content-Type: application/pdf` and real PDF bytes.

### 10. Frontend: types, API client, store
- **Files:** `web/src/types/index.ts` (edit: `OrderStatus`, `Supplier`, `OrderLineItem`, `Order`, `CreateOrderLineItemRequest`), `web/src/services/api.ts` (edit: `supplierApi`, `orderApi` incl. `downloadInvoice` with `responseType: 'blob'`), `web/src/stores/agro.ts` (edit: `suppliers`/`orders` state, `fetchSuppliers`/`createSupplier`/`fetchOrders`/`fetchOrderById`/`createOrder`/`placeOrder`/`receiveOrder`/`downloadOrderInvoice` actions — `receiveOrder` also calls `fetchInventory()` afterward, same "refresh stock after a stock-affecting action" convention as `approveStockAdjustment`).
- **No new test at this step** — covered by the e2e tests in step 11 (this repo doesn't unit-test Pinia stores in isolation; `app.spec.ts`/`gps-boundary.spec.ts` exercise stores only through the UI, consistent with the existing suite).

### 11. Frontend: `OrdersView.vue` + `OrderDetailModal.vue` + nav/route
- **Files:**
  - `web/src/views/OrdersView.vue` (new — list + KPIs + "+ New Order" modal with supplier select/inline "+ New supplier" + line-item builder, mirrors `InventoryView.vue`'s Add Item modal structure)
  - `web/src/components/modal/OrderDetailModal.vue` (new — status, line items, total, Place/Receive buttons gated by status, Download Invoice button)
  - `web/src/router/index.ts` (edit: `{ path: '/orders', name: 'orders', component: OrdersView, meta: { title: 'Orders', sub: 'PURCHASING', feature: 'inventory' } }`)
  - `web/src/components/layout/Sidebar.vue` (edit: new `ALL_NAV` entry `{ id: 'orders', path: '/orders', label: 'Orders', feature: 'inventory', badgeFn: () => null }`)
- **Test first:** new `web/e2e/orders.spec.ts` (see step 12 — written test-first per file, run against the not-yet-built view to confirm it fails for the right reason, then build the view to green).

### 12. E2E: `web/e2e/orders.spec.ts`
- **Fixtures:** extend `web/e2e/fixtures.ts` with `SUPPLIERS`/`ORDERS` fixture arrays (mirrors `INVENTORY_CATEGORIES`/`STOCK_ADJUSTMENTS`).
- **Cases:** create order (Draft) → appears in list; place a Draft order → status becomes Placed, Place button gone, Receive button shown; receive a Placed order → status becomes Received, inventory re-fetch reflects increased stock (mirrors `app.spec.ts`'s "approving updates stock" test shape); status-guard cases (Draft shows no Receive button, Received shows neither button); Download Invoice button fires a request to the invoice route (asserted via `page.route`/request interception, not real PDF bytes).
- Run `npx playwright test` after each red/green cycle; keep `app.spec.ts`/`gps-boundary.spec.ts` green throughout (nav-item-count assertions in `app.spec.ts` do **not** need updating since they assert a fixed subset of labels, not the full nav list — verify this holds once the new nav entry is added).

## Risks / dependencies

- Step 3 (extending `StockMovement.CreateIn`) touches a method the existing `RecordInventoryReceiptCommandHandler` already calls — must stay source-compatible (additive optional param only).
- Step 6 (`ReceiveOrderCommand`) is the highest-risk step: must not partially apply stock if a line item's `InventoryItem` can't be resolved. Resolve/validate **all** line items' `InventoryItem`s up front (before any `AddStock` call) so a bad line item fails atomically rather than leaving some items already restocked.
- `AgroMesh.Tests` gaining a `ProjectReference` to `AgroMesh.Infrastructure` (step 8) is a first for this repo — verify it doesn't drag in a runtime requirement (e.g. Npgsql/Redis connection) just from being referenced; QuestPDF itself has no such dependency, and the test only instantiates `OrderInvoiceService` directly, never `AgroMeshDbContext`.
- Migrations (step 9) require the local Postgres container; if unavailable, `dotnet ef migrations add` can still generate migration files (it only needs design-time DbContext, not a live DB) but applying/verifying them needs the running container confirmed already up in this session.
- `web/e2e/app.spec.ts`'s nav-list assertion (`navItems = ['Dashboard', ..., 'Reports']`) must be re-checked after adding the Orders nav entry — it currently asserts specific labels are present, not an exact count, so it should remain green, but this needs verifying, not assuming.
