# Orders, Suppliers & Invoices (Phase 4 of Inventory)

## Addendum — three live-testing bug fixes folded into this session

Mid-build, the requester live-tested the already-shipped Phases 1-3 (and this in-progress Phase 4)
against a rebuilt `agromesh_api` container and found three issues, explicitly brought in scope for
this session alongside the Phase 4 acceptance criteria below:

1. **Add Item quantity corruption (typed 1000, stored 999.980)** — root-caused via direct
   reproduction (a Playwright test driving the real form, plus direct `curl` calls against the live
   API with `quantity: 1000`) to a Chrome/browser footgun, not a domain or server bug: scrolling the
   mouse wheel while a `type="number"` input is still focused silently adjusts its value by one
   `step` per notch (the same as clicking the native spinner) — `1000 - 0.02` (two notches at
   `step="0.01"`) exactly matches the reported `999.980`. Both the frontend payload and the backend
   round-trip were confirmed byte-for-byte correct for a plain fill+submit before this was
   identified. Fixed via `@wheel.prevent` on every `type="number"` input in `web/src` (16 inputs
   across 6 files) — see `InventoryView.vue`'s doc comment. Regression coverage:
   `AgroMesh.Tests/InventoryTests.cs` (`Create_WholeNumberOpeningStock_IsStoredExactly`,
   `AddStock_WholeNumberQuantity_IsAddedExactly`) and a new Playwright test simulating the exact
   wheel-scroll-while-focused scenario (`web/e2e/app.spec.ts`).
2. **Pick Stock: auto-attribute the logged-in user instead of a manual Worker picker, and require a
   Section/Block destination.** `PickStockCommand` no longer accepts a client-supplied `WorkerId` —
   it now auto-attributes the authenticated caller via `ICurrentUserAccessor.UserId` (requiring
   `[Authorize]` on the Pick action specifically, mirroring the existing scoped-not-controller-wide
   gate already on `Adjust`), and now requires a `SectionId` (and `BlockId` when the section has
   blocks) recording where the picked stock is going. `StockMovement` gained `PerformedByUserId`,
   `SectionId`, `BlockId` columns (nullable, additive migration — mirrors how `WorkerId`/`OrderId`
   were each added in earlier phases); the pre-existing `WorkerId` column is left in place
   (unpopulated by this flow going forward) rather than a disruptive drop-column migration, since
   nothing else reads it either.
3. **Order creation was missing a price input — verified already resolved.** By the time this was
   raised, `OrdersView.vue`'s line-item builder already had a working, required `UnitPrice` number
   input (`placeholder="Unit ₦"`) wired into `CreateOrderCommand`'s payload, with `canSubmitCreate`
   gating the submit button on it — confirmed via source inspection and the already-passing
   `orders — create order modal builds a Draft order...` e2e test. No code change was needed; this
   was almost certainly observed against an earlier, incomplete build during active development.

Also fixed in the same pass, as pre-existing (predating this feature) defects surfaced while keeping
the full test suite green per this process's own exit criteria: `GetOrdersQueryHandler`'s N+1 query
pattern (now bulk-fetches suppliers/items once per farm and joins in memory, matching
`GetStockAdjustmentRequestsQueryHandler`'s established convention); `web/e2e/fixtures.ts`'s
`WORKERS`/`WORKER_ROLES`/`ACTIVITIES` fixtures still used a stale `dailyRate` field name from before
the "worker pay frequency" commit renamed it to `payRate`/`payFrequency`; and roughly a dozen stale
CSS-class selectors in `web/e2e/app.spec.ts`'s Dashboard/Weather/sidebar test sections referencing
class names (`.kpi-grid`, `.weather-temp`, `.season-badge`, `.spray-window`/`.spray-label`,
`.task-section`/`.task-due`/`.task-reasoning`, `.farm-sub`) that no longer exist after a prior
dashboard redesign — every one of these was confirmed via direct source inspection to be a genuine,
deterministic mismatch (not flaky timing), and none were introduced by this session's own changes.

## Goal

A farm user can place a purchase order with a supplier for one or more inventory items,
track it through Draft → Placed → Received, download a PDF invoice for it at any time, and —
when it's marked Received — have its line items automatically flow into stock as `In`
`StockMovement`s, increasing `CurrentStock` on each ordered `InventoryItem` exactly the way a
manual stock-in does today. This closes the loop from "we're low on X" (Phase 1's low-stock
alerts) to "we ordered more, it arrived, stock is updated" without a second manual data-entry
step.

## Context / existing code

Phases 1-3 (commit `5663075`) already built:

- `InventoryItem` (`api/AgroMesh.Domain/Entities/InventoryItem.cs`) — `AddStock`/`DeductStock`/`AdjustStock`, farm-scoped, `CurrentStock`/`ReorderThreshold`/`IsLowStock`.
- `InventoryCategory` (`api/AgroMesh.Domain/Entities/InventoryCategory.cs`) — the farm-scoped entity shape this feature's `Supplier` mirrors: `FarmId`, `Name`, private setters, static `Create`, its own repository (`InventoryCategoryRepository.cs`), EF configuration (`InventoryCategoryConfiguration.cs`), migration, `CreateInventoryCategoryCommand` + `GetInventoryCategoriesQuery`, and sibling controller `InventoryCategoriesController` at `api/farms/{farmId}/inventory-categories`.
- `StockMovement` (`api/AgroMesh.Domain/Entities/StockMovement.cs`) — `CreateIn`/`CreateOut`/`CreateAdjustment` factories; `CreateIn` currently links only to an optional `TransactionId`.
- `StockAdjustmentRequest` (`api/AgroMesh.Domain/Entities/StockAdjustmentRequest.cs`) — the precedent for an entity enforcing its own status-transition invariants (`Approve`/`Reject`, throws `DomainException` on an invalid transition) that `Order`'s `Place`/`Receive` mirror.
- `RecordInventoryReceiptCommand` (`api/AgroMesh.Application/Commands/Transaction/RecordInventoryReceiptCommand.cs`) — the exact precedent for turning "stock arrived" into an `In` movement: `item.AddStock(qty, notes)` → `_inventoryRepo.UpdateAsync(item)` → `StockMovement.CreateIn(...)` → `_inventoryRepo.AddMovementAsync(movement)`. No arithmetic duplicated, no nested `_mediator.Send`.
- `ICurrentUserAccessor` (`api/AgroMesh.Application/Interfaces/ICurrentUserAccessor.cs`) — `UserId`/`Role`/`IsAdmin`, JWT-claims backed, fails closed to `Member`.
- `Section.Crops` (`api/AgroMesh.Domain/Entities/Section.cs`, `api/AgroMesh.Infrastructure/Persistence/Configurations/SectionConfiguration.cs`) — the precedent for a `List<TValueObject>` mapped as a single `jsonb` column via `HasConversion` + `System.Text.Json`, which `Order.LineItems` mirrors.
- Money: no currency field anywhere in the domain; `Transaction.Amount`/`WorkerRole.PayRate` are plain `decimal`; the frontend renders money with a literal `₦` prefix + `.toLocaleString()` (`web/src/components/ui/TransactionRow.vue`, `WorkerRow.vue`). `Order`/`OrderLineItem` money fields follow this exactly.
- Auth: `InventoryAdjustmentsController` (`api/AgroMesh.API/Controllers/InventoryAdjustmentsController.cs`) is this repo's only precedent for a controller-level `[Authorize]` attribute; `InventoryController`/`InventoryCategoriesController` remain deliberately open. New `SuppliersController`/`OrdersController` follow the `[Authorize]`-at-controller-level pattern (any authenticated user — no role gate, unlike Phase 3's approve/reject).
- Frontend: `web/src/views/InventoryView.vue`'s "Add Item / Stock" modal is the precedent for inline-create-a-related-entity UX (create-new-category-inline while filling the main form) that the Orders page's inline-add-supplier mirrors. `web/src/stores/agro.ts` call-then-refetch action shape, `web/src/router/index.ts` + `web/src/components/layout/Sidebar.vue`'s `ALL_NAV` array for the new route/nav entry.
- Zero existing `Supplier`/`Order`/`Invoice`/`Purchase` concepts anywhere in the repo (confirmed via repo-wide grep) — this is genuinely greenfield on top of the above. No PDF library, no file-download endpoint, no CSV-export precedent exist either (Topbar's "Export CSV" button is dead UI with no handler).

## Requirements

1. A farm user can create a `Supplier` (`Name`, optional `Phone`/`Email`) scoped to a farm, and list a farm's suppliers.
2. A farm user can create an `Order` in `Draft` status for a farm, referencing a real `Supplier` and one or more line items (`InventoryItemId`, `Quantity > 0`, `UnitPrice >= 0`), each `InventoryItemId` referencing a real, farm-scoped `InventoryItem`.
3. An order's status only moves forward: `Draft → Placed` (`PlaceOrderCommand`) and `Placed → Received` (`ReceiveOrderCommand`). Placing a non-Draft order, or receiving a non-Placed order, throws a `DomainException` (422) and leaves state unchanged.
4. Marking an order `Received` converts every line item into an `In` `StockMovement` against the referenced `InventoryItem`, increasing its `CurrentStock` by the line's `Quantity` via the existing `InventoryItem.AddStock` (no new arithmetic). This must be atomic per order: it either fully succeeds or, if any line item resolves to a missing/foreign-farm `InventoryItem`, throws before mutating any stock.
5. Each such `StockMovement` records the originating order via a new nullable `OrderId` field on `StockMovement` (mirrors the existing nullable `TransactionId`/`WorkerId` fields), so movements are traceable back to the order that created them.
6. A farm user can fetch a farm's orders (optionally filtered by status) and a single order's detail (including resolved supplier name and line-item detail).
7. A farm user can download a PDF invoice for any order (`GET /api/farms/{farmId}/orders/{orderId}/invoice`), containing at minimum: farm name, supplier name/contact, order status, each line item (item name, quantity, unit price, line total), and the order total — generated via QuestPDF, returned as `application/pdf` bytes.
8. All new Orders/Suppliers endpoints require an authenticated caller (`[Authorize]`); no role/admin gate is required to place, receive, or view orders.
9. Every write endpoint enforces farm-scoping: a caller cannot place/receive/view an order (or its invoice) that belongs to a different farm than the one in the route (returns `DomainException`/404, not silently succeeding against another farm's data).
10. The frontend Orders page lists orders, supports creating a Draft order (supplier select with inline "+ New supplier", a line-item builder for item/qty/price), shows an order's detail with status and a "Download Invoice" button, and exposes Place/Receive actions gated by the order's current status (no Place button once Placed/Received; no Receive button unless Placed).

## Non-goals

- Editing/deleting an order or its line items once created (no `UpdateOrderCommand`) — a mis-entered Draft order must be recreated, not patched. Cancelling/voiding an order is out of scope.
- Editing/deleting a Supplier once created.
- Partial receipt (receiving only some line items, or receiving more/less than ordered) — `Receive()` always converts every line item's full ordered quantity.
- Any payment/accounts-payable tracking (order total is informational, not linked to `Transaction`/Finance).
- Multi-currency — this app has one implicit currency (₦), matching the rest of the codebase.
- Uploading a supplier-provided invoice/attachment — the PDF generated here is AgroMesh's own outgoing invoice/purchase-order document, not an imported file.
- Any change to the pre-existing, still-ungated `InventoryController`/`InventoryCategoriesController`/`InventoryAdjustmentsController` endpoints.
- Real second-user/second-role live e2e coverage for the `[Authorize]` gate (same limitation Phase 3 already documented) — covered by a handler-level test with a fake `ICurrentUserAccessor`/anonymous-request style check instead, consistent with how Phase 3 tested its admin gate.

## Acceptance criteria

- [x] `Supplier.Create` requires a non-blank `Name`; a farm can have multiple suppliers. (domain test)
- [x] `CreateSupplierCommand` persists a supplier scoped to `FarmId` and returns its DTO; `GetSuppliersQuery` returns only that farm's suppliers. (handler test)
- [x] `Order.Create` requires at least one line item, each with `Quantity > 0` and `UnitPrice >= 0`; rejects an empty list. (domain test)
- [x] A freshly-created `Order` has `Status == Draft` and `Total == Sum(Quantity * UnitPrice)` computed, not stored. (domain test)
- [x] `Order.Place()` transitions `Draft → Placed`; calling it again (or on a `Received` order) throws `DomainException` and leaves `Status` unchanged. (domain test)
- [x] `Order.Receive()` transitions `Placed → Received`; calling it on a `Draft` or already-`Received` order throws `DomainException` and leaves `Status` unchanged. (domain test)
- [x] `CreateOrderCommand` persists a Draft order referencing a real `Supplier`/`InventoryItem`s and returns its DTO with resolved supplier/item names. (handler test)
- [x] `PlaceOrderCommand` on a Draft order updates its status to Placed; on a non-Draft order throws `DomainException` with zero side effects (no repository update call). (handler test)
- [x] `ReceiveOrderCommand` on a Placed order: (a) calls `InventoryItem.AddStock` for every line item, increasing each item's `CurrentStock` by exactly its line quantity; (b) records one `In` `StockMovement` per line item with `OrderId` set to the order's id; (c) sets the order's status to Received. (handler test)
- [x] `ReceiveOrderCommand` on a Draft order throws `DomainException` with zero stock/movement side effects (no `InventoryItem` mutated, no movement recorded). (handler test)
- [x] `ReceiveOrderCommand` on an already-Received order throws `DomainException` with zero side effects (no double-application of stock). (handler test)
- [x] `GetOrdersQuery` returns a farm's orders, optionally filtered by status; `GetOrderByIdQuery` returns a single farm-scoped order's detail or throws when the id belongs to a different farm. (handler test)
- [x] The invoice-generation service produces non-empty PDF bytes (`%PDF` magic-number header) for a real `Order`/`Supplier`/`Farm`. (infra test, real QuestPDF call — no fake)
- [x] `SuppliersController`/`OrdersController` endpoints are decorated `[Authorize]` at the controller level (verified by inspecting the compiled attribute, mirroring how the existing controllers are structured) and none of Phase 1-3's `InventoryController`/`InventoryCategoriesController`/`InventoryAdjustmentsController` gained new `[Authorize]` attributes as a side effect.
- [x] `GET /api/farms/{farmId}/orders/{orderId}/invoice` returns `Content-Type: application/pdf` with a `Content-Disposition`/filename convention (`invoice-{orderId}.pdf`).
- [x] Frontend: Orders page creates a Draft order (supplier select + inline "+ New supplier" + line-item builder) and the new order appears in the list. (Playwright e2e)
- [x] Frontend: order detail's Place button transitions Draft → Placed and disappears once Placed (Receive button appears instead). (Playwright e2e)
- [x] Frontend: order detail's Receive button transitions Placed → Received, and the app's inventory state reflects the increased stock afterward (mirrors Phase 2/3's "pick reduces stock"/"approve applies stock" e2e assertions). (Playwright e2e)
- [x] Frontend: Receive/Place buttons are correctly gated — a Draft order shows no Receive button, a Received order shows neither. (Playwright e2e)
- [x] Frontend: "Download Invoice" triggers a request to the invoice endpoint (asserted via the mocked route firing with the right method/URL — Playwright cannot assert real PDF bytes end-to-end against a mocked API). (Playwright e2e)
- [x] `dotnet test` and `npm run test:e2e` (or equivalent Playwright command) are fully green, including all pre-existing Phase 1-3 tests (no regressions).

### Addendum acceptance criteria (the three live-testing bugs)

- [x] Typing a whole number (e.g. "1000") into any `type="number"` input in the app and then scrolling the mouse wheel while that input is still focused does not change its value — verified for the Add Item Quantity field specifically (`@wheel.prevent` applied app-wide). (Playwright e2e + domain regression test)
- [x] `PickStockCommand` no longer accepts or requires a client-supplied `WorkerId`; it auto-attributes `ICurrentUserAccessor.UserId` to the resulting `StockMovement.PerformedByUserId`, throwing `DomainException` with zero side effects if no authenticated user can be identified. (handler test)
- [x] `PickStockCommand` requires a `SectionId` (farm-scoped, IDOR-guarded) and records it on the `StockMovement`; a `BlockId` is validated to belong to the given section when supplied. (handler test)
- [x] The `POST .../inventory/{itemId}/pick` endpoint requires `[Authorize]`; no other `InventoryController` action gained a new `[Authorize]` as a side effect.
- [x] Frontend: the Pick Stock modal shows the authenticated user read-only (no worker dropdown), requires selecting a Section, and requires a Block when the selected section has any. (Playwright e2e)
- [x] Frontend: the Orders create-order line-item builder has a working, required `UnitPrice` input wired into the create-order request, and the submit button is reachable/enabled once a valid line item is built. (Playwright e2e — confirmed already passing, no code change needed)

## Test plan

- **Domain unit tests** (`AgroMesh.Tests`, plain xUnit, `Assert.*`, no mocking library): `Supplier.Create` validation; `Order.Create`/`Place`/`Receive` status-transition invariants and the computed `Total`.
- **Handler/integration tests** (`AgroMesh.Tests`, hand-rolled fake repositories mirroring `StockAdjustmentApprovalTests.cs`'s `FakeInventoryRepository` pattern — no in-memory EF, no mocking library): `CreateSupplierCommand`, `GetSuppliersQuery`, `CreateOrderCommand`, `PlaceOrderCommand` (happy path + invalid-transition-throws-with-no-side-effects), `ReceiveOrderCommand` (happy path with movement/stock assertions + Draft-throws + already-Received-throws, each proving zero side effects on the failure paths), `GetOrdersQuery`/`GetOrderByIdQuery` (farm-scoping negative case).
- **Infra test** (new: a project reference from `AgroMesh.Tests` to `AgroMesh.Infrastructure`, since this is the first thing worth testing that isn't fake-able and has no DB dependency): the real QuestPDF-backed invoice service actually produces valid, non-empty PDF bytes.
- **API layer**: `[Authorize]` presence is verified via a plain reflection/attribute check in a unit test (no ASP.NET Core `TestServer`/`WebApplicationFactory` precedent exists in this repo to stand up for one feature) plus a manual live-verification pass against local Postgres (`docker-compose`, port 5433, `agromesh_api` container) hitting the real endpoints end-to-end (create supplier → create/place/receive order → confirm stock increased → download invoice, confirm bytes are a real PDF) — matching this repo's established coverage shape.
- **Playwright e2e** (`web/e2e/orders.spec.ts`, new file — the Orders flow is large enough to warrant its own file rather than growing `app.spec.ts` further, following the existing precedent of `gps-boundary.spec.ts` being split out): create order → place → receive → stock-increase assertion → status-gated buttons → invoice download request assertion. Mocked HTTP throughout, same convention as `app.spec.ts`/`gps-boundary.spec.ts`.
