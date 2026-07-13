# Inventory Phase 2 — Manual Pick / Issue Stock

## Goal

Farm staff often pull stock (chemicals, fuel, tools, parts) straight off the shelf for a job
without that job ever being logged as a formal Activity — a fuel top-up for a generator, a spot
tool loan, a supervisor grabbing chemical for an off-book spray. Today the only way `CurrentStock`
moves down is the automatic, name-matched deduction that runs when an Activity is logged
(`IInventoryDeductionService`), and that path is deliberately lenient (it *warns* on insufficient
stock rather than blocking, because an activity should still get logged even if the inventory
bookkeeping is imperfect). This phase adds a second, manual "Pick / Issue Stock" action on the
Inventory page so a user can record a real-time, standalone stock withdrawal — who took it, how
much, and why — that unconditionally blocks if it would push stock negative, and separately warns
(without blocking) if it pushes stock to or below the item's reorder threshold.

## Context / existing code (reuse, don't rebuild)

- **Domain**: `api/AgroMesh.Domain/Entities/InventoryItem.cs:51-59` — `DeductStock(decimal quantity)`
  already throws `DomainException` for `quantity <= 0` or insufficient stock; reused as-is, no new
  arithmetic written anywhere in this phase. `api/AgroMesh.Domain/Entities/StockMovement.cs` —
  `CreateOut(farmId, inventoryItemId, quantity, activityLogId=null, notes=null)` is the existing
  factory for `Out` movements; extended (not replaced) with an optional `workerId` parameter, since
  there is currently no field anywhere on the entity for "who" performed a movement.
  `api/AgroMesh.Domain/Entities/Worker.cs` — real entity with `FarmId`/`Name`/`Active`, already has
  `IWorkerRepository`.
- **The auto-deduction path this stays separate from**: `IInventoryDeductionService` /
  `api/AgroMesh.Infrastructure/Services/InventoryDeductionService.cs:20-54`, sole caller
  `LogActivityCommandHandler` (`api/AgroMesh.Application/Commands/Activity/LogActivityCommand.cs:77`).
  It resolves items by name and **catches `DomainException` into a soft warning string** on
  insufficient stock (lines 46-50) instead of blocking. The new command never calls this service and
  never catches `DomainException` around `DeductStock` — it lets the exception propagate to
  `api/AgroMesh.API/Middleware/ExceptionMiddleware.cs:40-54`, which already turns any unhandled
  `DomainException` into an HTTP 422 `application/problem+json` body (`title: "Unprocessable
  Request"`, `detail: ex.Message`).
- **"Who" — reused convention**: `LogActivityCommand.cs:49-55` builds `WorkerLog` entries by calling
  `_workerRepo.GetByIdWithRoleAsync(w.WorkerId, ct)` for each worker id supplied by the client. The
  new command mirrors this: a required (at the request-validation level) `WorkerId`, resolved via
  `IWorkerRepository.GetByIdAsync`, validated for existence and farm-scoping — mirroring the IDOR
  guard in `CreateInventoryItemCommand.cs`'s `ResolveTargetItemAsync` (`existing.FarmId != farmId`
  check). `ICurrentUserAccessor` (the office/company-user "who") is deliberately **not** used here —
  a stock pick is a farm-labor action performed by a `Worker`, not an office login, and
  `InventoryController` has no `[Authorize]` today (pre-existing, out-of-scope gap, unchanged by this
  phase — see Non-goals).
- **Repository**: `api/AgroMesh.Application/Interfaces/IInventoryRepository.cs` — `GetByIdAsync`,
  `UpdateAsync`, `AddMovementAsync` reused as-is, no interface changes. `IWorkerRepository.GetByIdAsync`
  reused as-is.
- **Sibling command to mirror**: `api/AgroMesh.Application/Commands/Inventory/AdjustInventoryStockCommand.cs`
  (route param → command → single domain call → movement → persist shape) and
  `CreateInventoryItemCommand.cs` (IDOR guard + `DomainException` "not found" pattern).
- **API**: `api/AgroMesh.API/Controllers/InventoryController.cs` — `api/farms/{farmId}/inventory`
  (`GET /`, `POST /`, `GET /low-stock`, `GET /{itemId}/movements`, `PATCH /{itemId}/adjust`). New
  route: `POST /{itemId}/pick` (action-suffixed sub-route under an item id like `/adjust`, but `POST`
  since every call creates a new movement rather than overwriting a value — not idempotent). No
  `[Authorize]` added (same pre-existing gap, left alone).
- **Response shape convention**: `LogActivityCommand`'s `LogActivityResponse(activity, warnings)` and
  `InventoryDeductionService` returning warning strings inline — followed here via
  `PickStockResponse(InventoryItemDto Item, string? LowStockWarning)` so the frontend gets the
  post-deduction state and any warning in one round trip.
- **Frontend**: `web/src/views/InventoryView.vue` — KPI grid, filter pills, item table with
  `.inv-actions` (currently just `Adjust`), Adjust modal and Add Item modal both built from the same
  `.modal-overlay`/`.modal`/`.modal-header`/`.modal-title`/`.modal-body`/`.modal-footer`/`.form-group`/
  `.form-label`/`.form-input`/`.form-select`/`.error-msg` classes (already defined ~lines 401-410) —
  the new Pick Stock modal reuses all of these, zero new layout CSS beyond one small `.warn-msg`
  class for the low-stock warning (same visual language as the existing `.badge-warn` color).
  `web/src/stores/agro.ts` — `adjustStock` (~line 326) is the action shape mirrored for `pickStock`;
  `fetchWorkers`/`store.workers` (existing Workers feature) reused for the "Picked by" dropdown, no
  new worker-fetching path invented. `web/src/services/api.ts:101-107` — `inventoryApi` gets a new
  `pick(itemId, body)` method alongside `getAll`/`getLowStock`/`create`/`adjust`/`getMovements`.
- **E2E**: `web/e2e/app.spec.ts` — Inventory block (~389-640) gets a new
  `// ── Inventory: Pick / Issue Stock (Phase 2) ──` sub-block after the existing Phase 1 sub-block.
  `web/e2e/fixtures.ts` — `INVENTORY`/`INVENTORY_CATEGORIES` reused unchanged; `WORKERS` (existing
  Workers-feature fixture, already farm-scoped to `farm_001` with `id`/`name`) reused for the "Picked
  by" dropdown instead of inventing new worker fixture data. `mockApi()`'s existing
  `**/api/farms/farm_001/workers**` intercept already covers the new modal's worker fetch.
- **Domain unit tests**: `api/AgroMesh.Tests/InventoryTests.cs` (existing `DeductStock` coverage,
  unchanged — still the regression proof that the block-on-insufficient-stock behavior works). New
  `api/AgroMesh.Tests/StockMovementTests.cs` (no such file exists yet) covers the new `WorkerId`
  parameter on `CreateOut`, following the precedent Phase 1 set with `InventoryCategoryTests.cs` for
  a new domain concept.

## Requirements

1. `POST api/farms/{farmId}/inventory/{itemId}/pick` accepts `{ quantity, workerId, purpose }` and:
   a. Resolves the `InventoryItem` by `itemId`, 404-style `DomainException` if missing or if its
      `FarmId` doesn't match the route's `farmId` (IDOR guard, mirrors `CreateInventoryItemCommand`).
   b. Resolves the `Worker` by `workerId`, `DomainException` if missing or if its `FarmId` doesn't
      match the route's `farmId` (same IDOR guard, applied to the worker id).
   c. Requires a non-blank `purpose` — `DomainException` if blank/whitespace-only (request-level
      validation; `Notes` itself stays optional at the entity level so `CreateIn`/`CreateAdjustment`
      callers are unaffected).
   d. Calls `item.DeductStock(quantity)` — the existing domain method — with **no** try/catch around
      it. An insufficient-stock or non-positive-quantity `DomainException` propagates unhandled,
      producing an HTTP 422 via the existing `ExceptionMiddleware`. No partial deduction is possible:
      `DeductStock` either fully applies or throws before mutating `CurrentStock`.
   e. Persists the updated item (`IInventoryRepository.UpdateAsync`) and records exactly one `Out`
      `StockMovement` via `StockMovement.CreateOut(farmId, itemId, quantity, notes: purpose,
      workerId: worker.Id)`.
   f. Returns `PickStockResponse { Item: InventoryItemDto, LowStockWarning: string? }` — the DTO
      reflects post-deduction `CurrentStock`/`IsLowStock`; `LowStockWarning` is a non-null message
      (same wording style as `InventoryDeductionService`'s low-stock string) when the resulting
      `item.IsLowStock` is true, else `null`. This is a **warning, not a block** — a pick that lands
      exactly on or below the threshold still succeeds.
2. `StockMovement` gains an optional `WorkerId` (nullable string column, `HasMaxLength(50)`,
   unindexed — movements are looked up by item, not by worker, in this phase). `CreateIn` and
   `CreateAdjustment` are unaffected (no worker on those movement types). A new EF Core migration
   adds the column; applied against the local Postgres instance.
3. This is a fully separate code path from `IInventoryDeductionService`: the new command/handler
   does not call it, is not called by it, and does not replicate its catch-and-warn behavior around
   `DomainException`. `InventoryDeductionService.cs` is not modified by this phase.
4. Inventory page: a "Pick Stock" button appears in each row's `.inv-actions`, next to the existing
   "Adjust" button. Clicking it opens a modal (reusing the existing modal CSS classes) with: current
   available stock (read-only), a quantity input, a "Picked by" worker `<select>` (sourced from
   `store.workers`, reusing the existing Workers fetch — not a new endpoint), and a required purpose
   text input.
5. Submitting the modal calls the new endpoint. On success:
   - The inventory list refreshes (same `fetchInventory()`-after-mutation pattern as `adjustStock`)
     so the row's displayed stock and any `LOW STOCK` badge reflect the pick immediately.
   - If the response carries a `LowStockWarning`, it's shown inline in the modal (distinct styling
     from the error case) and the modal stays open until the user dismisses it (`Done` button) —
     this deliberately differs from the silent auto-close used when there's no warning, so the
     warning is not missed.
   - If there's no warning, the modal closes immediately (mirrors the existing Adjust modal's
     success behavior).
6. On a 422 (insufficient stock, or any other domain rejection), the modal shows the server's error
   message inline (reusing the existing `.error-msg`/`extractErrorMessage` pattern from the Add Item
   modal) and stays open with the user's input intact — no partial/negative stock is ever displayed,
   because the backend never mutated it.
7. The Pick Stock submit button is disabled client-side unless quantity is a positive number, a
   worker is selected, and purpose is non-blank — this is a UX nicety, not a substitute for the
   authoritative server-side validation in Requirement 1.

## Non-goals (explicitly out of scope for this phase)

- Any approval workflow for stock picks (Phase 3) — a pick is recorded and applied immediately,
  with no pending/approved/rejected state.
- Orders / invoices / purchasing (Phase 4).
- Adding `[Authorize]` to `InventoryController` — pre-existing, repo-wide gap, unchanged (same
  stance as Phase 1).
- Editing or reversing a past pick (no "undo" / delete-movement flow) — corrections go through the
  existing Adjust Stock flow, unchanged by this phase.
- Displaying "picked by" on the movements history UI — `GET /{itemId}/movements` and
  `StockMovementDto` are unchanged by this phase; `WorkerId` is captured and persisted but not
  surfaced back through that read path (a natural Phase 2.1 follow-up, not required by the stated
  goal of *capturing* who/quantity/purpose).
- Handler/controller-level automated tests (mocking or `WebApplicationFactory`/in-memory EF) — this
  repo has zero precedent for that test style (confirmed again for this phase); the IDOR guards and
  full request/response contract are instead proven via Playwright e2e (mocked HTTP) plus a manual
  verification pass against the real local Postgres + running API, consistent with Phase 1.
- Reusing/modifying `IInventoryDeductionService` or its catch-and-warn behavior in any way — this
  phase is explicitly a second, independent code path (Requirement 3).

## Acceptance criteria

- [x] **AC1** — `StockMovement.CreateOut(...)` accepts an optional `workerId` and sets it on the
      returned instance; when omitted (existing `InventoryDeductionService` call site), `WorkerId` is
      `null` — no regression on the auto-deduction path's movements. *(domain unit test —
      `StockMovementTests.cs`: `CreateOut_SetsWorkerIdWhenProvided`,
      `CreateOut_WorkerIdDefaultsToNullWhenOmitted`, `CreateOut_SetsCoreFields`)*
- [x] **AC2** — `InventoryItem.DeductStock` (unchanged, reused) still throws `DomainException` for
      insufficient stock and for non-positive quantity — regression proof that the new command has
      nothing new to break here. *(domain unit test, regression — `InventoryTests.cs`, unchanged,
      both facts still green; also independently reconfirmed live via manual curl — see below)*
- [x] **AC3** — E2E: clicking "Pick Stock" on a row opens a modal titled with the item's name,
      showing available stock, a quantity input, a "Picked by" worker select populated from
      `store.workers`, and a purpose input. *(`app.spec.ts` — "Pick Stock button opens the pick modal
      with worker options")*
- [x] **AC4** — E2E: submitting a valid pick (quantity within stock, worker selected, purpose filled)
      posts `{ quantity, workerId, purpose }` to `POST .../inventory/{itemId}/pick`, the modal closes,
      and the row's displayed stock reflects the reduced quantity from the refreshed list. *(`app.spec.ts`
      — "a valid pick reduces stock, records who/quantity/purpose, and closes the modal")*
- [x] **AC5** — E2E: submitting a pick whose quantity exceeds current stock surfaces the backend's
      422 error message inline in the modal (`.error-msg`) and the modal stays open — mirrors AC13 of
      Phase 1's Add Item error-handling test, proving the block behavior is visible to the user.
      *(`app.spec.ts` — "Pick Stock shows an inline error and stays open when quantity exceeds
      current stock")*
- [x] **AC6** — E2E: submitting a pick that pushes the resulting stock to/below the item's reorder
      threshold shows an inline low-stock warning (`.warn-msg`, distinct element/class from
      `.error-msg`) inside the modal, and the modal does **not** auto-close — the user must click
      "Done" — proving the warning (§3 of the goal) is distinguishable from the block (§5/AC5).
      *(`app.spec.ts` — "Pick Stock shows a low-stock warning (not a block) when stock crosses the
      threshold")*
- [x] **AC7** — E2E: the Pick Stock submit button is disabled until quantity, worker, and purpose are
      all filled in (client-side UX guard from Requirement 7). *(`app.spec.ts` — "Pick Stock submit is
      disabled until quantity, worker, and purpose are all filled")*
- [x] **AC8** — Manual verification: applied the new EF Core migration
      (`AddStockMovementWorkerId`) against the local Postgres instance — confirmed via `\d
      "StockMovements"` that a nullable `WorkerId character varying(50)` column exists, no data loss.
      Live `curl` pass against the rebuilt `agromesh_api` container proved: (a) a valid pick reduces
      `CurrentStock` (100L → 35L) and records an `Out` movement with the given `WorkerId`/`Notes`
      (confirmed via direct `SELECT` on `StockMovements`); (a2) a pick crossing the reorder threshold
      (35L → 25L, threshold 30) returns `lowStockWarning` non-null with `isLowStock: true`, HTTP 200
      (not blocked); (b) an over-quantity pick (1000L requested, 25L available) returns 422
      `"Insufficient stock..."` and a re-`GET` confirms `CurrentStock` is unchanged at 25L; (c) an
      item id from a different farm than the route, and a worker id from a different farm than the
      route, are both rejected 422 "not found" (IDOR guard, tested independently for both ids); (d)
      blank purpose, blank `workerId`, non-existent `workerId`, non-existent item id, and
      zero-quantity are all independently rejected 422 with the expected message, reusing
      `DeductStock`'s own guard for the quantity case. Full command transcript in the build summary.
- [x] **AC9** — `dotnet build` succeeds for the whole solution and `dotnet test` is fully green —
      68/68 (65 pre-existing + 3 new `StockMovementTests.cs` cases), no regressions.
- [x] **AC10** — The new command/handler contains no call to `IInventoryDeductionService` and no
      `try`/`catch` around `DeductStock` — verified by code review (`grep -n "IInventoryDeductionService\|try\s*{"`
      against `PickStockCommand.cs` returns only the doc-comment's *mention* of the service name, no
      actual reference or catch block).

## Test plan

- **Domain unit tests** (`api/AgroMesh.Tests/`, xUnit, plain `Assert.*`, no mocks/in-memory EF —
  matching `InventoryTests.cs`/`InventoryCategoryTests.cs`): new `StockMovementTests.cs` covering
  AC1 (workerId set when provided, null when omitted) plus basic field-assignment coverage for
  `CreateOut` (no existing test file touches this entity at all today). `InventoryTests.cs` stays
  unchanged as regression coverage for AC2 (the block behavior this phase depends on).
- **Handler/controller/integration tests**: none added, consistent with this repo's precedent (see
  Non-goals) — the IDOR guards (item-farm-scope, worker-farm-scope) and the "let DomainException
  propagate" behavior are proven via e2e (mocked HTTP, frontend contract) and the manual live-API
  pass (AC8), not via handler-level automated tests.
- **Playwright e2e** (`web/e2e/app.spec.ts`, new `// ── Inventory: Pick / Issue Stock (Phase 2) ──`
  sub-block; `web/e2e/fixtures.ts` `INVENTORY`/`WORKERS` reused unchanged): AC3, AC4, AC5, AC6, AC7.
  Edge/negative cases specifically covered: over-quantity pick (422, block), threshold-crossing pick
  (warning, not a block — modal stays open with a *different* affordance than the error case),
  and the disabled-submit-until-valid guard.
- **Manual/live verification** (not part of the automated suite, run once against the reachable
  local Postgres at `localhost:5433` + running API, reported factually in the final summary): apply
  the migration, confirm the new column, and `curl` through the happy path, the over-quantity block,
  and the cross-farm IDOR rejection (AC8).

**Final results**: `dotnet test` 68/68 green (65 pre-existing + 3 new). `npx vue-tsc --noEmit` clean.
Full `npx playwright test` (both spec files, 100 tests): **79 passed, 21 failed** — all 5 new Pick
Stock tests pass, all 24 Inventory-tagged tests (19 Phase 1 + 5 Phase 2) pass, and the 21 failures
are the exact same pre-existing, unrelated Dashboard/Sidebar/Workers/log-activity-worker-picker/
back-navigation staleness already documented in Phase 1's spec.md (confirmed by exact test-name
diff against that baseline — no new failures introduced). Manual live-API verification (AC8) run
against the rebuilt `agromesh_api` Docker container and local Postgres, full transcript in the
build summary.
