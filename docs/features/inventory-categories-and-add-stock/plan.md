# Plan — Inventory Phase 1

Ordered so each step has a red→green cycle. Domain steps first (they're what's actually unit
tested), then infrastructure/migration, then application/API, then frontend, then e2e last (e2e
exercises everything end-to-end so it naturally comes after the stack it's testing exists).

## Status: all 11 steps complete

All steps below were completed as planned, with one deviation from the plan discovered during
Step 10/11: `web/e2e/app.spec.ts`'s shared `mockApi()` helper never seeded an authenticated session,
so every test hitting a non-public route (including the new Inventory tests) was failing at the
router guard before reaching the page under test. This predates this feature (confirmed via
`git log` — introduced when the auth-gated router was added two commits ago, with only
`gps-boundary.spec.ts` ever updated to seed a session). Fixed `mockApi()` to do the same
(`page.addInitScript` seeding `agro_token`/`agro_user`/`agro_active_farm_id`, plus mocking the bare
`GET /api/farms` the Sidebar calls on mount) — necessary for my own acceptance criteria, and it also
happened to restore most of the pre-existing suite. A separate, smaller set of pre-existing failures
remained after that fix (stale selectors/fixture field names on Dashboard/Sidebar/Workers, e.g.
`.kpi-grid` → now `.kpi-row`, `.farm-sub` → now `.farm-meta`/`.farm-area`, fixture `dailyRate` → now
`payRate`/`payFrequency`) — these are unrelated to Inventory and out of scope for this phase; see
spec.md Non-goals. All 19 Inventory-tagged e2e tests (11 pre-existing + 8 new) and the full domain
xUnit suite (65/65) are green.

## Step 1 — `InventoryCategory` domain entity

- **Files**: new `api/AgroMesh.Domain/Entities/InventoryCategory.cs`; delete
  `api/AgroMesh.Domain/Enums/InventoryCategory.cs`.
- **Test (red first)**: new `api/AgroMesh.Tests/InventoryCategoryTests.cs` —
  `Create_ValidName_SetsFields`, `Create_BlankName_ThrowsDomainException`,
  `CreateDefaults_ReturnsEightDefaultCategories` (asserts the exact name set from spec.md
  Requirement 3, all `IsDefault == true`, all `FarmId == farmId`).
- Mirrors `WorkerRole.cs` shape (`BaseEntity`, private setters, static `Create`) plus a
  `CreateDefaults(farmId)` static helper and a `DefaultCategoryNames` readonly array.

## Step 2 — `InventoryItem` switches from enum to FK + nav

- **Files**: `api/AgroMesh.Domain/Entities/InventoryItem.cs` (`CategoryId` string + `Category` nav
  replacing the enum property; `Create(...)` takes `categoryId`), `api/AgroMesh.Tests/InventoryTests.cs`
  (update the one `InventoryItem.Create(...)` call site to pass a category id string instead of the
  enum value).
- **Test**: existing `InventoryTests.cs` tests must still pass unchanged (regression) — this is the
  red→green step: change the call site (red, since `InventoryCategory.Chemical` no longer compiles),
  then confirm all 5 tests are green again.
- Risk: this is the one place a compile break can cascade widely — do this step right after Step 1
  and rebuild immediately to catch every downstream compile error in one pass rather than piecemeal.

## Step 3 — Fix every other compile-time consumer of the old enum

- **Files**: `api/AgroMesh.Application/DTOs/InventoryDtos.cs` (`InventoryItemDto.Category` →
  `CategoryId`/`CategoryName`; `CreateInventoryItemRequest.Category` → `CategoryId`, plus new
  `Quantity`/`ExistingItemId` fields per spec Requirement 7), `api/AgroMesh.Infrastructure/Services/Scheduling/ClaudeSchedulingService.cs:155`
  (`i.Category == InventoryCategory.Chemical` → `i.Category.Name == "Chemical"`).
- **Test**: `dotnet build` is the acceptance gate here (no new behavior yet, just making the tree
  compile again) — confirm 0 errors before moving on.

## Step 4 — Infrastructure: EF configuration, repository, DI

- **Files**:
  - new `api/AgroMesh.Application/Interfaces/IInventoryCategoryRepository.cs` (mirrors
    `IWorkerRoleRepository`: `GetByIdAsync`, `GetByFarmIdAsync`, `FindByNameAsync`, `AddAsync`,
    `AddRangeAsync`).
  - new `api/AgroMesh.Infrastructure/Persistence/Repositories/InventoryCategoryRepository.cs`
    (mirrors `WorkerRoleRepository.cs`).
  - new `api/AgroMesh.Infrastructure/Persistence/Configurations/InventoryCategoryConfiguration.cs`
    (mirrors `WorkerRoleConfiguration.cs`: `HasMany(c => c.Items).WithOne(i => i.Category)
    .HasForeignKey(i => i.CategoryId).OnDelete(Restrict)`, indexes on `FarmId` and `(FarmId, Name)`).
  - `api/AgroMesh.Infrastructure/Persistence/Configurations/InventoryItemConfiguration.cs` (drop the
    old `Category` property config + its index; add `CategoryId` maxlength-50 required + new
    `(FarmId, CategoryId)` index — relationship itself is configured on the category side).
  - `api/AgroMesh.Infrastructure/Persistence/Repositories/InventoryRepository.cs` (`.Include(i =>
    i.Category)` on `GetByIdAsync`/`GetByFarmIdAsync`/`FindByNameAsync`/`GetLowStockAsync` — single
    eager-load join, not N+1).
  - `api/AgroMesh.Infrastructure/Persistence/AgroMeshDbContext.cs` (`DbSet<InventoryCategory>
    InventoryCategories`).
  - `api/AgroMesh.Domain/Entities/Farm.cs` (`List<InventoryCategory> InventoryCategories` nav, for
    convention parity with `Sections`/`WorkerRoles`/`InventoryItems`, enabling the FK).
  - `api/AgroMesh.Infrastructure/DependencyInjection.cs:36-61` (`services.AddScoped<IInventoryCategoryRepository,
    InventoryCategoryRepository>();` next to `IInventoryRepository`).
- **Test**: `dotnet build` green. No unit test here (pure plumbing, no business logic) — consistent
  with how `SectionRepository`/`WorkerRoleRepository` etc. have no dedicated tests either.

## Step 5 — EF Core migration (structure + data backfill)

- Scaffold with `dotnet ef migrations add AddInventoryCategories` from `api/AgroMesh.API` (start
  project) against `api/AgroMesh.Infrastructure`, then hand-edit the generated file to sequence:
  1. `CreateTable InventoryCategories` (+ FK to `Farms`, + indexes).
  2. `Sql(...)` — seed the 8 default categories for every row currently in `Farms`.
  3. `AddColumn CategoryId` to `InventoryItems` as **nullable** first (can't be `NOT NULL` against
     existing rows without a value).
  4. `Sql(...)` — backfill `CategoryId` per the old `Category` int value using the mapping table in
     spec.md Requirement 4, scoped by `FarmId` to the just-seeded categories.
  5. `AlterColumn CategoryId` to `NOT NULL`.
  6. Drop the old `Category` column + its `IX_InventoryItems_FarmId_Category` index.
  7. Add `IX_InventoryItems_FarmId_CategoryId` + `FK_InventoryItems_InventoryCategories_CategoryId`
     (`Restrict`).
  - `Down()` reverses all of the above (drop FK/index/table, re-add nullable `Category` int column,
    reverse-map backfill, `NOT NULL`, re-add old index) so the migration is round-trippable.
- **Verification**: apply against the reachable local Postgres (`localhost:5433`,
  `agromesh_db` container, already healthy) with `dotnet ef database update`, then query
  `InventoryCategories`/`InventoryItems` directly to confirm: 8 rows per farm, every existing item
  has a non-null `CategoryId` that resolves to a real category row. This is the closest thing to
  "testing" a data migration in a repo with no migration-test harness — reported as manual
  verification, not an automated test, per the Test plan in spec.md.
- Risk: this is the step most likely to need iteration (raw SQL quoting/casing on Postgres is
  fiddly) — budget extra red→green cycles here specifically against the live DB rather than trying
  to get it perfect blind.

## Step 6 — Application layer: category list/create + unified item stock-in

- **Files**:
  - new `api/AgroMesh.Application/DTOs/InventoryCategoryDtos.cs` (or appended to `InventoryDtos.cs`
    — decide based on which reads cleaner once written; `InventoryCategoryDto(Id, FarmId, Name,
    IsDefault)`, `CreateInventoryCategoryRequest(Name)`).
  - new `api/AgroMesh.Application/Queries/Inventory/GetInventoryCategoriesQuery.cs` (mirrors
    `GetWorkerRolesQuery.cs`, minus the per-item count join since `ItemCount` isn't required).
  - new `api/AgroMesh.Application/Commands/Inventory/CreateInventoryCategoryCommand.cs` (mirrors
    `CreateWorkerRoleCommand.cs`).
  - `api/AgroMesh.Application/Commands/Inventory/CreateInventoryItemCommand.cs` — rewritten per spec
    Requirement 7: resolve target item (`ExistingItemId` → name match → new), validate unit match on
    an existing target, `AddStock` either way, always `AddMovementAsync(StockMovement.CreateIn(...))`.
  - `api/AgroMesh.Application/Queries/Inventory/GetInventoryQuery.cs` — map `CategoryId`/`CategoryName`
    from the now-included `Category` nav.
- **Test**: no handler tests added (see spec.md Test plan) — this step's correctness is proven by
  the e2e tests in Step 8 plus the manual curl pass in Step 5's verification. `dotnet build` +
  `dotnet test` (full domain suite) must stay green throughout.

## Step 7 — API controllers

- **Files**: new `api/AgroMesh.API/Controllers/InventoryCategoriesController.cs` (mirrors
  `WorkerRolesController.cs`, route `api/farms/{farmId}/inventory-categories`, `GET`/`POST`).
  `InventoryController.cs` itself needs no route changes (create endpoint behavior changes
  internally only).
- **Test**: manual curl smoke test against the live API once migrated (Step 5's DB is a prerequisite)
  — confirm `GET` returns the 8 seeded categories for a real farm id, `POST` creates a 9th.

## Step 8 — Frontend: types, api client, store

- **Files**:
  - `web/src/types/index.ts` — `InventoryCategory` becomes an interface (`{id, farmId, name,
    isDefault}`); `InventoryItem.category` → `categoryId`/`categoryName`.
  - `web/src/services/api.ts` — new `inventoryCategoryApi` (`getAll`/`create`); `inventoryApi.create`
    finally called from somewhere (the store action added below).
  - `web/src/stores/agro.ts` — new `inventoryCategories` state, `fetchInventoryCategories()`,
    `createInventoryCategory(name)`, `addOrRestockInventoryItem(payload)` (mirrors `adjustStock`'s
    call-then-refresh shape); `fetchInventory()` gets a `try { loading.value = true; error.value =
    null; ... } finally { loading.value = false }` wrapper (the bug fix from spec Requirement 11) —
    scoped to this function only, not every `fetch*` in the store (see spec Non-goals).
- **Test**: no Vitest in this repo (only Playwright + `vue-tsc` type-check + build) — `npm run
  build` (`vue-tsc && vite build`) is the gate for this step, confirming the type changes are
  internally consistent before wiring the view.

## Step 9 — Frontend: `InventoryView.vue` Add Item modal + dynamic filter pills

- **Files**: `web/src/views/InventoryView.vue` — dynamic `filters` computed from
  `store.inventoryCategories` (plus `'All'`), new "+ Add Item" button, new modal (mirrors the
  existing Adjust modal's classes exactly) with Category select (+ inline "+ New Category"), Item
  picker (+ "Create new item" default), Name/Unit/Quantity/Reorder Threshold fields, inline
  `.error-msg` (mirrors `AddFarmModal.vue`'s `extractErrorMessage` usage) on submit failure,
  "Loading categories…" state.
- **Test**: `npm run build` green; full behavioral coverage comes from Step 10's e2e tests (this repo
  has no component-test runner to red/green against directly, so the modal is built to satisfy the
  e2e specs written in Step 10 — see risk note below).

## Step 10 — E2E tests (red → green against the real build)

- **Files**: `web/e2e/fixtures.ts` (`INVENTORY_CATEGORIES` fixture; update `INVENTORY` items to
  carry `categoryId`/`categoryName` instead of `category`), `web/e2e/app.spec.ts` (`mockApi()` gets
  the new categories route; new tests for AC7–AC13; existing AC14 regression tests updated only
  where the fixture shape changed, not their assertions).
- **Test/verification loop**: write each new test first against the *old* (not-yet-built) UI to
  confirm it fails for the right reason (element not found), then build the modal/store pieces from
  Step 9 until it's green, per feature (Add button → modal opens → category select → item picker →
  submit-new → submit-restock → inline category creation → error state → loading/empty regressions).
  Run `npx playwright test` after each green test, not just at the end, to catch cross-test
  regressions early (`workers: 1`/`fullyParallel: false` in `playwright.config.ts` means tests run
  sequentially, so a shared-state bug surfaces immediately rather than being masked by parallelism).

## Step 11 — Full regression pass

- `dotnet build` (whole solution), `dotnet test` (full xUnit suite), `npm run build` (type-check +
  vite build), `npx playwright test` (full e2e suite, not just the new inventory tests — confirms no
  other page broke from the shared `types/index.ts` or `stores/agro.ts` changes).

## Risks / dependencies

- **Migration data backfill (Step 5)** is the highest-risk step — hand-written SQL against Postgres
  column casing/quoting, and it's the one place a mistake could silently strand existing items
  without a category. Mitigated by having a live local Postgres to actually apply it against and
  query afterward, rather than trusting the migration blind.
- **Step 2 is a deliberate single big compile break** (enum → FK) — every downstream file that
  references `InventoryCategory` breaks at once. This is intentional: fixing it in one pass (Steps
  2–3) avoids a half-migrated state where some code expects the enum and some expects the entity.
- **No component/unit test runner for Vue** — Step 9's UI work is only provably correct once Step
  10's e2e tests are green; there's a wider red window on the frontend than the backend's tighter
  per-file red→green cycles. Mitigated by writing the e2e specs immediately after (not long after)
  the store/api plumbing in Step 8, before the modal markup, so "red for the right reason" is
  confirmed before implementation.
- **`InventoryItemDto` shape change** (`Category` → `CategoryId`/`CategoryName`) is a breaking wire
  contract change with exactly two producers (`CreateInventoryItemCommandHandler`,
  `GetInventoryQueryHandler`) and any frontend code reading `item.category` — all are inventoried in
  Step 3/8, no other consumer exists per the repo-wide grep in spec.md.
