# Inventory Phase 2 — Implementation Plan

Ordered so each step has one red→green cycle and the suite stays green after every step.

## Step 1 — Domain: `StockMovement.WorkerId` (red → green)

- **Red**: add `api/AgroMesh.Tests/StockMovementTests.cs` with:
  - `CreateOut_SetsWorkerIdWhenProvided`
  - `CreateOut_WorkerIdDefaultsToNullWhenOmitted` (regression for the existing
    `InventoryDeductionService` call site, which never passes one)
  - `CreateOut_SetsCoreFields` (farmId/inventoryItemId/quantity/type=Out/notes — no existing test
    file covers `StockMovement` at all)
  Run `dotnet test` → new tests fail to compile/fail (no `WorkerId` param yet).
- **Green**: `api/AgroMesh.Domain/Entities/StockMovement.cs` — add `public string? WorkerId { get; private set; }`
  and an optional trailing `string? workerId = null` parameter to `CreateOut`, setting it on the
  returned instance. `CreateIn`/`CreateAdjustment` untouched.
- Run `dotnet test` → all green.

## Step 2 — Infrastructure: EF config + migration

- `api/AgroMesh.Infrastructure/Persistence/Configurations/StockMovementConfiguration.cs` — add
  `builder.Property(m => m.WorkerId).HasMaxLength(50);` (nullable, no `.IsRequired()`, no new index
  — movements aren't queried by worker in this phase).
- `dotnet ef migrations add AddStockMovementWorkerId --project AgroMesh.Infrastructure --startup-project AgroMesh.API`
  from `api/`. Review the generated `Up`/`Down` (should be a single nullable `ADD COLUMN`).
- `dotnet ef database update --project AgroMesh.Infrastructure --startup-project AgroMesh.API`
  against the local Postgres (`localhost:5433`, already running via docker-compose) — confirms the
  migration applies cleanly to real data (16 farms / existing StockMovements rows, if any).
- `dotnet build` for the whole solution.

## Step 3 — Application: DTOs + `PickStockCommand`

- `api/AgroMesh.Application/DTOs/InventoryDtos.cs` — add:
  ```csharp
  public record PickStockRequest(decimal Quantity, string WorkerId, string Purpose);
  public record PickStockResponse(InventoryItemDto Item, string? LowStockWarning);
  ```
- New file `api/AgroMesh.Application/Commands/Inventory/PickStockCommand.cs`:
  `PickStockCommand(string FarmId, string InventoryItemId, PickStockRequest Request) : IRequest<PickStockResponse>`.
  Handler (constructor-injects `IInventoryRepository`, `IWorkerRepository`):
  1. Validate `Request.WorkerId`/`Request.Purpose` non-blank → `DomainException` if not.
  2. `GetByIdAsync(InventoryItemId)` → `DomainException` if null or `item.FarmId != FarmId` (mirrors
     `CreateInventoryItemCommand.ResolveTargetItemAsync`'s IDOR guard).
  3. `_workerRepo.GetByIdAsync(Request.WorkerId)` → `DomainException` if null or
     `worker.FarmId != FarmId` (same guard, applied to the worker).
  4. `item.DeductStock(Request.Quantity)` — **no try/catch**. Then `UpdateAsync(item)`.
  5. `StockMovement.CreateOut(item.FarmId, item.Id, Request.Quantity, notes: Request.Purpose,
     workerId: worker.Id)` → `AddMovementAsync`.
  6. Build `InventoryItemDto` from the post-deduction `item` (same shape
     `CreateInventoryItemCommand` returns); `LowStockWarning` = the low-stock message string when
     `item.IsLowStock`, else `null`.
  No new test at this layer per repo precedent (see spec's Test plan) — proven via e2e + manual
  curl, and via the domain-level `DeductStock`/`CreateOut` tests already in place.

## Step 4 — API: wire the endpoint

- `api/AgroMesh.API/Controllers/InventoryController.cs` — add:
  ```csharp
  [HttpPost("{itemId}/pick")]
  public async Task<IActionResult> Pick(string farmId, string itemId, [FromBody] PickStockRequest request, CancellationToken ct) =>
      Ok(await _mediator.Send(new PickStockCommand(farmId, itemId, request), ct));
  ```
- `dotnet build` for the whole solution; `dotnet test` full green.

## Step 5 — Manual verification against the live local API (AC8)

- Restart/rebuild the `agromesh_api` container (or run `dotnet run` against the same connection
  string) so it picks up the new code + migration.
- `curl` sequence: (a) a valid pick on a real item/worker pair → 200, reduced `CurrentStock`,
  `LowStockWarning` present/absent as expected; (b) an over-quantity pick → 422,
  `CurrentStock` unchanged (re-`GET` to confirm); (c) a `workerId`/`itemId` belonging to a different
  farm than the route → rejected (404-style `DomainException`).
- Record the exact commands/responses in the final report — not claimed as automated coverage.

## Step 6 — Frontend: types, api client, store action

- `web/src/types/index.ts` — add `PickStockResponse { item: InventoryItem; lowStockWarning: string | null }`.
- `web/src/services/api.ts` — `inventoryApi.pick = (itemId, body) => api.post(`/api/farms/${FARM_ID}/inventory/${itemId}/pick`, body)`.
- `web/src/stores/agro.ts` — `pickStock(itemId, body)`: calls `inventoryApi.pick`, then
  `await fetchInventory()` (mirrors `adjustStock`'s shape exactly), returns `res.data as PickStockResponse`
  so the view can read `lowStockWarning` without a second round trip.

## Step 7 — Frontend: `InventoryView.vue` Pick Stock modal (red → green via e2e)

- **Red**: add the new e2e sub-block to `web/e2e/app.spec.ts` (AC3-AC7) — run `npx playwright test`
  filtered to the new tests, confirm they fail (no button/modal exists yet).
- **Green**:
  - Add a `Pick Stock` button in `.inv-actions`, before/after the existing `Adjust` button.
  - Add state: `pickTarget`, `pickQty`, `pickWorkerId`, `pickPurpose`, `picking`, `pickError`,
    `pickWarning`.
  - `openPick(item)` — resets fields, ensures `store.workers` is populated (fetch if empty, plus an
    eager `store.fetchWorkers()` added to `onMounted` alongside the existing
    `fetchInventory`/`fetchInventoryCategories`/`fetchChemicals` calls so the dropdown is ready by
    the time a user opens the modal in normal use).
  - `canSubmitPick` computed — quantity > 0, `pickWorkerId` set, `pickPurpose` non-blank (AC7).
  - `submitPick()` — calls `store.pickStock`; on success, if `res.lowStockWarning` set
    `pickWarning` (modal stays open, footer swaps to a single "Done" button); else close the modal
    immediately. On failure, `pickError = extractErrorMessage(e, ...)` (modal stays open, inputs
    intact) — mirrors the Add Item modal's `submitError` handling exactly.
  - Modal markup reuses `.modal-overlay`/`.modal`/`.modal-header`/`.modal-title`/`.modal-body`/
    `.modal-footer`/`.form-group`/`.form-label`/`.form-input`/`.form-select`/`.error-msg`. One new
    scoped style, `.warn-msg` (same color as the existing `.badge-warn`), for the low-stock warning.
  - Run the new e2e sub-block again → green. Run the *entire* Inventory block (Phase 1 + Phase 2) to
    confirm no regressions from the new button/markup.

## Step 8 — Full regression pass + self-review

- `dotnet test` (whole solution).
- `npx vue-tsc --noEmit` (type-check, since the plan touches `.vue`/`.ts` files).
- `npx playwright test` — at minimum the full `app.spec.ts` file; compare pass/fail counts against
  the Phase 1 baseline to confirm no new regressions beyond the already-known, pre-existing
  Dashboard/Sidebar/Workers staleness documented in Phase 1's spec.
- Self-review against the checklist in the top-level instructions (correctness, tests, security/IDOR,
  performance, consistency, regressions) — particularly: does the handler genuinely avoid catching
  `DomainException` (AC10)? Is the worker farm-scope guard actually exercised by a test path (even if
  only manual/e2e)? Does the negative-quantity/zero-quantity case reuse `DeductStock`'s existing
  guard rather than re-implementing it?

## Data model / migration summary

| Entity | Change | Migration |
|---|---|---|
| `StockMovement` | + nullable `WorkerId` (string, maxlength 50) | `AddStockMovementWorkerId` |

No other entities change. No new tables.

## Risks / dependencies

- **Local Postgres availability** for the migration-apply + manual curl steps — confirmed running
  (`agromesh_db` healthy on `localhost:5433`) before starting; if it becomes unavailable mid-build,
  the migration is still generated and reviewed (build/test don't require a live DB), and AC8's
  manual verification is reported as blocked/skipped rather than faked.
- **`store.workers` staleness** — if a farm has zero active workers, the "Picked by" select has no
  options and `canSubmitPick` can never be true; this is expected/acceptable (matches how the
  existing Log Activity modal already requires at least one worker) and not a bug to fix here.
- **Ordering of the new button vs. `Adjust`** — existing e2e selectors use
  `page.locator('button', { hasText: 'Adjust' })`, a substring match; adding a `Pick Stock` button
  anywhere in `.inv-actions` doesn't collide with that selector, confirmed by reading the existing
  tests before writing new markup.
