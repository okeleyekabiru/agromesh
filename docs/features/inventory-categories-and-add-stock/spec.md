# Inventory Phase 1 — Dynamic Categories + Add Item / Add Stock

## Goal

Farm managers currently track inventory (chemicals, fertilizer, fuel, etc.) against a hardcoded,
farm-agnostic `InventoryCategory` enum, and there is no way to add a brand-new item or top up an
existing item's stock from the UI — the "Adjust Stock" modal only lets you *overwrite* a known
item's quantity, and the backend `create` endpoint is wired up but never called. This phase makes
categories a farm-owned, extensible list (seeded with sensible defaults, growable per farm) and
gives the Inventory page a real "Add Item" entry point that both onboards new items and restocks
existing ones — always leaving a proper `In` movement behind so stock history stays trustworthy.

## Context / existing code (reuse, don't rebuild)

- **Domain**: `api/AgroMesh.Domain/Entities/InventoryItem.cs` (`AddStock`/`DeductStock`/`AdjustStock`,
  static `Create`), `api/AgroMesh.Domain/Entities/StockMovement.cs` (`CreateIn`/`CreateOut`/`CreateAdjustment`),
  `api/AgroMesh.Domain/Enums/InventoryCategory.cs` (the enum being replaced).
- **Application**: `api/AgroMesh.Application/Commands/Inventory/CreateInventoryItemCommand.cs` (create,
  currently no movement recorded), `.../AdjustInventoryStockCommand.cs` (absolute-quantity correction,
  unchanged by this phase), `.../Transaction/RecordInventoryReceiptCommand.cs` (reference
  `AddStock` + `StockMovement.CreateIn` shape), `Queries/Inventory/GetInventoryQuery.cs`,
  `Interfaces/IInventoryRepository.cs` (`FindByNameAsync`, `AddMovementAsync` already exist and are
  reused as-is), `DTOs/InventoryDtos.cs`.
- **API**: `api/AgroMesh.API/Controllers/InventoryController.cs` — `api/farms/{farmId}/inventory`
  (`GET /`, `POST /`, `GET /low-stock`, `GET /{itemId}/movements`, `PATCH /{itemId}/adjust`). No
  `[Authorize]` today (pre-existing gap on this controller, explicitly left alone — see Non-goals).
- **Sibling entity to mirror for the new Category entity**: `WorkerRole` is the closest existing
  "farm-scoped lookup list with a list+create endpoint" — `Domain/Entities/WorkerRole.cs`,
  `Application/Interfaces/IWorkerRoleRepository.cs`, `Infrastructure/Persistence/Repositories/WorkerRoleRepository.cs`,
  `Infrastructure/Persistence/Configurations/WorkerRoleConfiguration.cs`,
  `Application/Commands/WorkerRole/CreateWorkerRoleCommand.cs`,
  `Application/Queries/Worker/GetWorkerRolesQuery.cs`, `API/Controllers/WorkerRolesController.cs`
  (route `api/farms/{farmId}/worker-roles`, sibling to `api/farms/{farmId}/workers`) — the new
  `InventoryCategoriesController` mirrors this exactly, sibling to `InventoryController` at
  `api/farms/{farmId}/inventory-categories`. `Worker.Role` (required nav + `WorkerRoleId` FK) is the
  pattern mirrored for `InventoryItem.Category` (required nav + `CategoryId` FK).
- **Infrastructure**: `Persistence/Configurations/InventoryItemConfiguration.cs`, `WorkerRoleConfiguration.cs`,
  `Persistence/Repositories/InventoryRepository.cs`, `WorkerRoleRepository.cs`, `DependencyInjection.cs:36-61`,
  migrations in `Persistence/Migrations/` (latest `20260712190057_AddWorkerPayFrequency.cs`). Local
  Postgres reachable at `localhost:5433` (docker-compose `agromesh_db`, healthy) — migrations are
  generated **and applied** against it in this run; see Test plan.
- **Consumer that breaks on the type change**: `Services/Scheduling/ClaudeSchedulingService.cs:155`
  (`inventory.Where(i => i.Category == InventoryCategory.Chemical ...)`) — fixed to compare
  `i.Category.Name == "Chemical"`. Repo-wide grep for `InventoryCategory` confirms exactly 7
  reference sites before this change (`InventoryDtos.cs` x2, `InventoryItem.cs` x2, the enum file
  itself, `ClaudeSchedulingService.cs`, `InventoryTests.cs`) — all updated; no other consumer exists.
- **Tests**: `api/AgroMesh.Tests/InventoryTests.cs` (domain-only, no mocking framework, no in-memory
  EF — the only test style in this repo; there are **no** handler/controller-level tests anywhere
  today, e.g. `RegisterCommand`/`CreateFarmCommand`/`CreateWorkerRoleCommand` have none either — see
  Test plan for how Phase 1 stays consistent with that).
- **Frontend**: `web/src/views/InventoryView.vue` (KPI cards, hardcoded `filters` array, item rows,
  Adjust modal — `.modal-overlay`/`.modal`/`.modal-header`/`.modal-title`/`.modal-body`/`.modal-footer`/
  `.form-group`/`.form-label`/`.form-input`/`.form-select` classes reused, no new modal styling
  invented), `stores/agro.ts` (`fetchInventory` never toggles `loading` today — real, silent bug;
  `adjustStock`/`addStockFromReceipt` are the action shape mirrored), `services/api.ts` (`inventoryApi.create`
  exists but is dead code — wired up), `types/index.ts` (`InventoryCategory` string union → replaced),
  `web/src/components/modal/AddFarmModal.vue` (mirrored for the `.error-msg` + `extractErrorMessage`
  inline-error pattern, since the existing Adjust modal has no error UI to copy from).
- **E2E**: `web/e2e/app.spec.ts` inventory block (~line 374), fed by `web/e2e/fixtures.ts` `INVENTORY`.
  `mockApi()` helper extended with the new categories endpoint.

## Requirements

1. Replace `AgroMesh.Domain.Enums.InventoryCategory` (enum) with `AgroMesh.Domain.Entities.InventoryCategory`
   (entity: `Id`, `FarmId`, `Name`, `IsDefault`, `CreatedAt`/`UpdatedAt` via `BaseEntity`).
2. `InventoryItem.Category` becomes a required nav property backed by `CategoryId` (FK), replacing
   the enum property. `InventoryItem.Create(...)` takes a `categoryId` string instead of an enum value.
3. Seed 8 default categories per farm: **Chemical, Fertilizer, Farm Tools, Seed, Fuel, Equipment,
   Spare Parts, Other**. The first 6 are the literal list requested; **Spare Parts** and **Other**
   are added so the two enum values with no direct counterpart in the requested list
   (`SpareParts`, `Other`) still have a home — see the mapping table below. This runs (a) once as a
   data migration for every farm that exists today, and (b) for every new farm at creation time
   (`RegisterCommand` and `CreateFarmCommand`, the two places `Farm.Create` is called).
4. Existing `InventoryItem` rows are migrated from their old integer `Category` enum value to the
   matching new category's `Id`, scoped to the item's own farm, per this table:

   | Old enum value | Old ordinal | New category name    |
   |----------------|-------------|-----------------------|
   | Chemical       | 0           | Chemical              |
   | Fertilizer     | 1           | Fertilizer             |
   | Seed           | 2           | Seed                   |
   | Fuel           | 3           | Fuel                   |
   | Equipment      | 4           | Equipment              |
   | SpareParts     | 5           | Spare Parts            |
   | Other          | 6           | Other                  |

   No existing item loses its category. `Farm Tools` has no enum counterpart — it exists purely as
   a default for items added going forward.
5. `GET api/farms/{farmId}/inventory-categories` returns every category for the farm (defaults +
   any farm has added). `POST api/farms/{farmId}/inventory-categories` creates a new farm-scoped
   category (`{ name }` body → `InventoryCategoryDto`). Mirrors `WorkerRolesController` exactly
   (route shape, controller shape, no auth added/removed — see Non-goals).
6. `InventoryItemDto` exposes `CategoryId` and `CategoryName` (string) instead of the enum `Category`.
7. The existing `POST api/farms/{farmId}/inventory` (`CreateInventoryItemCommand`) becomes the single
   general-purpose, non-transaction-linked "stock in" path, used both for brand-new items and for
   restocking, and **always** records a `StockMovement` (`CreateIn`) for the quantity added —
   including the initial-stock case, which today silently records no movement (bug fixed as part of
   this phase). Concretely:
   - If `request.ExistingItemId` is supplied, that item is restocked (looked up by id; 404-style
     `DomainException` if it doesn't belong to a real item).
   - Else if an item with the same name already exists on the farm (case-insensitive,
     `IInventoryRepository.FindByNameAsync`, reused as-is), that item is restocked instead of a
     duplicate being created — this is the "by name match" path.
   - Else a brand-new `InventoryItem` is created (via `InventoryItem.Create(..., currentStock: 0, ...)`)
     and immediately stocked via the *same* `AddStock` call used for restocking, so both paths share
     one code path and one validation rule (quantity must be `> 0`, enforced by `AddStock` itself —
     no new arithmetic is written, per the "reuse `AddStock`/`DeductStock`" constraint).
   - In every branch, exactly one `StockMovement.CreateIn` is recorded via the existing
     `AddMovementAsync`, and `CurrentStock` only ever changes through `AddStock`.
   - If the resolved item's `Unit` doesn't match the submitted `Unit` (case-insensitive), the
     request is rejected with a `DomainException` (422) rather than silently letting stock counts
     get mixed across units (e.g. "kg" quantity added to an "L"-tracked item) — this is the phase's
     one new validation rule, layered on top of (not inside) `AddStock`.
8. The Inventory page gets an "Add Item" entry point (button) that opens a modal (mirroring the
   existing Adjust modal's markup/CSS classes) with: Category select (including an inline
   "+ New Category" affordance that creates a category via requirement 5 and selects it), an Item
   picker (defaults to "+ Create new item"; picking an existing item locks Name/Unit to that item —
   the "explicit item picker" path from requirement 7), Name, Unit, Quantity, and Reorder Threshold
   fields (the latter two hidden/not applicable when an existing item is picked, since that item
   already has its own threshold, editable only via the existing Adjust flow).
9. The category filter pills on the Inventory page are sourced from the fetched category list
   (`store.inventoryCategories`), not the hardcoded `filters` array.
10. Low-stock alerts (`store.lowStockCount`, the `LOW STOCK` badge, the alert banner) keep working
    unchanged — they depend on `IsLowStock`/`CurrentStock`/`ReorderThreshold`, none of which this
    phase touches.
11. Loading/error/empty states are added to everything touched: `fetchInventory` actually toggles
    `store.loading` (fixing the existing silent bug — the view's `v-if="store.loading"` has never
    fired), clears any stale `store.error` on a fresh attempt, the Add Item modal shows a real inline
    error message (not just `console.error`) on a failed submit, and the category select shows a
    "Loading categories…" state if opened before the categories fetch resolves.

## Non-goals (explicitly out of scope for this phase)

- Stock-out / issue flow (Phase 2).
- Any approval workflow for stock changes (Phase 3).
- Orders / invoices / purchasing (Phase 4).
- Adding `[Authorize]` to `InventoryController` (or the new `InventoryCategoriesController`) — this
  is a pre-existing, repo-wide gap on this controller; fixing it is a drive-by outside this phase's
  scope per the task brief.
- Category rename/delete/deactivate endpoints — only list + create, per the stated requirement.
- Editing an existing item's category, unit, or reorder threshold from the new Add Item modal — that
  remains the job of the existing Adjust Stock modal (unchanged) for anything other than restocking.
- Server-side duplicate-category-name prevention — mirrors `WorkerRole`'s existing precedent (no
  dedup logic there either); the UI's category dropdown already surfaces existing names, so
  accidental duplicates require deliberately re-typing an existing name.
- Handler-level or controller-level automated tests — see Test plan for why, and what covers this
  instead.
- Fixing the shared `loading`/`error` ref design for `fetchSections`/`fetchWorkers`/`fetchActivities`/
  `fetchTransactions` (same pre-existing pattern, same bug class, but not part of what this phase
  touches — only `fetchInventory` is fixed).
- Fixing pre-existing, unrelated `app.spec.ts` test failures on Dashboard/Sidebar (e.g.
  `.kpi-grid` vs the current `.kpi-row` markup, `.farm-sub` vs the current `.farm-meta`/`.farm-area`
  markup) discovered while running the full suite for regression purposes. These predate this
  feature (stale since the dashboard/sidebar UI was reworked in earlier commits, confirmed via
  `git log`) and are unrelated to Inventory — fixing them is a separate, unscoped effort. The
  `mockApi()` auth-seeding fix above was necessary (my own tests hit the same guard); rewriting
  Dashboard/Sidebar test assertions to match refactored markup is not, and is left as a known,
  pre-existing gap for the maintainer to address separately.

## Acceptance criteria

- [x] **AC1** — `InventoryCategory.Create(farmId, name)` validates a non-blank name and sets
      `FarmId`/`Name`/`IsDefault` (default `false` unless explicitly seeded). *(domain unit test —
      `InventoryCategoryTests.Create_ValidName_SetsFields`,
      `Create_IsDefaultTrue_SetsIsDefault`, `Create_BlankName_ThrowsDomainException`,
      `Create_TrimsName`)*
- [x] **AC2** — `InventoryCategory.CreateDefaults(farmId)` returns exactly the 8 categories listed in
      Requirement 3, each `IsDefault == true` and scoped to `farmId`. *(domain unit test —
      `InventoryCategoryTests.CreateDefaults_ReturnsEightDefaultCategories`)*
- [x] **AC3** — `InventoryItem.Create` accepts a `categoryId` string (not the old enum) and the value
      round-trips through `.CategoryId`; all 5 pre-existing `InventoryTests.cs` behaviors
      (`AddStock`/`DeductStock`/`IsLowStock`) still pass unchanged. *(domain unit test, regression —
      `InventoryTests.cs`, all 6 facts green)*
- [x] **AC4** — Creating a brand-new item through the unified stock-in path results in
      `CurrentStock == quantity` and exactly one `In` movement for that quantity. *(verified via
      manual curl against the live local Postgres-backed API — see Test plan — and via e2e
      "add item modal creates a brand-new item and it appears in the list")*
- [x] **AC5** — `GET /api/farms/{farmId}/inventory-categories` and
      `POST /api/farms/{farmId}/inventory-categories` exist, are wired through MediatR the same way
      as every other list+create pair in the app, and are exercised by e2e mocks (AC9) plus a manual
      curl pass against the live local Postgres-backed API (see Test plan — both verbs verified
      working against 16 real farms).
- [x] **AC6** — Existing inventory items keep their category after the migration runs (no item ends
      up with a null/missing `CategoryId`) — verified by applying the migration against the local
      Postgres instance (16 farms → 128 seeded categories, 8/farm) and confirming `CategoryId` is
      `NOT NULL` with an enforced FK to `InventoryCategories` (no rows existed to backfill in this
      environment, but the backfill SQL path was written and reviewed against the documented mapping
      table; see Test plan for what "verified" means here vs. what's asserted by an automated test).
- [x] **AC7** — E2E: submitting the Add Item modal with a name that does *not* match any existing
      item creates a new item (POST fires with no `existingItemId`) and it appears in the rendered
      list. *(`app.spec.ts` — "add item modal creates a brand-new item and it appears in the list")*
- [x] **AC8** — E2E: submitting the Add Item modal having picked an existing item via the picker
      restocks it — the POST payload carries that item's id and the row's displayed stock reflects
      the refreshed total after the mocked response. *(`app.spec.ts` — "add item modal restocks an
      existing item via the item picker")*
- [x] **AC9** — E2E: the category select offers "+ New Category"; choosing it reveals a name input;
      submitting it calls `POST .../inventory-categories` and the new category becomes the selected
      value without the user having to re-open the dropdown. *(`app.spec.ts` — "add item modal can
      create a new category inline and auto-selects it")*
- [x] **AC10** — E2E: the filter pills rendered on the page match the farm's fetched categories
      (not the old hardcoded `['All','Chemical','Fertilizer','Seed','Fuel','Equipment','SpareParts']`
      array) — a category present only in the mocked categories fixture (not in the old hardcoded
      list) renders as a pill. *(`app.spec.ts` — "category filter pills are sourced from fetched
      categories, not hardcoded", using "Farm Tools")*
- [x] **AC11** — E2E: a delayed `GET .../inventory` mock shows the "Loading inventory…" state before
      resolving (regression test for the fetch-never-toggles-loading bug). *(`app.spec.ts` —
      "loading state shown while the inventory list is in flight")*
- [x] **AC12** — E2E: an empty `GET .../inventory` mock still renders the "No inventory items" empty
      state (unchanged markup, still reachable). *(`app.spec.ts` — "empty state shown when there are
      no items")*
- [x] **AC13** — E2E: a mocked 422 failure on `POST .../inventory` (e.g. simulating the unit-mismatch
      `DomainException`) shows an inline error message inside the Add Item modal and the modal stays
      open (not just a console error). *(`app.spec.ts` — "add item modal shows an inline error on a
      failed submit and stays open")*
- [x] **AC14** — E2E: existing low-stock badge/banner/KPI/filter-by-Chemical/filter-by-Fuel/Adjust
      modal tests (already in `app.spec.ts`) keep passing unchanged against the new category DTO
      shape — regression coverage for Requirement 10. All 11 pre-existing Inventory tests pass
      unchanged (assertions untouched; only the fixture's `category` string field became
      `categoryId`/`categoryName` to match the new DTO shape).
- [x] **AC15** — `dotnet build` succeeds for the whole solution and `dotnet test` is fully green
      (65/65: 57 pre-existing + 8 new — 6 `InventoryCategoryTests` + 2 new/updated cases in
      `InventoryTests.cs`).

**Discovered along the way (not an acceptance criterion, but material to "full suite green"):**
`web/e2e/app.spec.ts`'s shared `mockApi()` helper never seeded an authenticated session
(`agro_token`/`agro_user`/`agro_active_farm_id` in `localStorage`), so every test hitting a
non-public route has been silently broken since the router's auth guard was introduced two commits
ago (`288db92`) — confirmed via `git log` that only `gps-boundary.spec.ts`'s one auth-requiring test
was ever updated to do this. Since my new Inventory tests hit the same auth-gated `/inventory`
route, this had to be fixed for my own acceptance criteria to be provable, so `mockApi()` now seeds
a session the same way `gps-boundary.spec.ts` already does (Test plan/Non-goals below cover what
this did and did not fix).

## Test plan

- **Domain unit tests** (`api/AgroMesh.Tests/`, xUnit, plain `Assert.*`, no mocks/no in-memory EF —
  matching `InventoryTests.cs`/`PolygonValidatorTests.cs`/`FarmSectionCreationRuleTests.cs`):
  new `InventoryCategoryTests.cs` covering AC1/AC2 (including negative case: blank/whitespace name
  throws `DomainException`), plus the updated `InventoryTests.cs` call site for AC3.
- **Handler/controller/integration tests**: **none added**, consistent with this repo's existing
  precedent — there are zero handler or controller tests anywhere today (`CreateWorkerRoleCommand`,
  `RegisterCommand`, `CreateFarmCommand`, `AdjustInventoryStockCommand` etc. are all untested at that
  layer). Introducing a new test *style* (mocking framework or `WebApplicationFactory`/in-memory EF)
  for just this feature would be inconsistent with the rest of the codebase, so Phase 1 instead
  relies on: domain-level unit tests for business rules (above), Playwright e2e tests that exercise
  the full request/response contract through mocked HTTP routes (below), and a manual verification
  pass against the real local Postgres + running API (reported in the final summary, not claimed as
  automated coverage).
- **Playwright e2e** (`web/e2e/app.spec.ts`, extending the existing `// ── Inventory ──` block,
  fixtures added to `web/e2e/fixtures.ts`, `mockApi()` extended for the categories endpoint): AC7,
  AC8, AC9, AC10, AC11, AC12, AC13, AC14 above. Edge/negative cases specifically covered: submitting
  with an existing-name match (restock, not duplicate), a 422 backend rejection surfacing as an
  inline error without closing the modal, and the loading/empty states for the list itself.
- **Manual/live verification** (not part of the automated suite, run once against the reachable
  local Postgres at `localhost:5433` and reported factually): apply the new EF Core migration,
  confirm `InventoryCategories` is seeded 8-per-farm and every pre-existing `InventoryItem` retains
  a valid `CategoryId`, and smoke-test the two new endpoints with `curl`.

**Final results**: `dotnet test` 65/65 green. `npx vue-tsc --noEmit` and `npm run build` clean.
Full `npx playwright test` (both spec files, 95 tests): **73 passed, 1 flaky-then-passed, 21 failed**
— all 19 Inventory-tagged tests (11 pre-existing + 8 new) pass; all 21 failures are the pre-existing,
unrelated Dashboard/Sidebar/Workers staleness described above (confirmed by exact selector/field-name
diffs against current markup — 3 distinct root causes: `.kpi-grid`→`.kpi-row` and friends on
Dashboard/Sidebar, `dailyRate`→`payRate` on the Workers fixture, and one navigation test asserting
the pre-redirect URL `/` instead of the now-correct `/dashboard`).
