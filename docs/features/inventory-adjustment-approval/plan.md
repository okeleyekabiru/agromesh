# Inventory Phase 3 — Implementation Plan

Ordered so each step has one red→green cycle and the suite stays green after every step.
Baseline captured before starting: `dotnet test` 68/68 green; Playwright baseline captured
separately and diffed against post-change runs (see Step 10).

## Step 1 — Domain: `UserRole` + `User.Role` (red → green)

- **Red**: new `api/AgroMesh.Tests/UserTests.cs` — `Create_DefaultsRoleToAdmin`. Run `dotnet test` →
  fails to compile (no `Role` member yet).
- **Green**: `api/AgroMesh.Domain/Enums/UserRole.cs` — `public enum UserRole { Admin, Member }`.
  `api/AgroMesh.Domain/Entities/User.cs` — add `public UserRole Role { get; private set; } =
  UserRole.Admin;`, explicit `Role = UserRole.Admin` in `Create(...)` for clarity.
- Run `dotnet test` → green.

## Step 2 — Domain: `StockAdjustmentStatus` + `StockAdjustmentRequest` (red → green)

- **Red**: new `api/AgroMesh.Tests/StockAdjustmentRequestTests.cs`:
  - `Create_SetsStatusToPending`
  - `Create_BlankNotes_ThrowsDomainException`
  - `Create_NegativeRequestedQuantity_ThrowsDomainException`
  - `Approve_SetsStatusAndReviewFields`
  - `Approve_AlreadyReviewed_ThrowsDomainException`
  - `Reject_SetsStatusAndReviewFields`
  - `Reject_AlreadyReviewed_ThrowsDomainException`
  Run `dotnet test` → fails (no entity yet).
- **Green**: `api/AgroMesh.Domain/Enums/StockAdjustmentStatus.cs` — `{ Pending, Approved, Rejected }`.
  `api/AgroMesh.Domain/Entities/StockAdjustmentRequest.cs` — entity + `Create`/`Approve`/`Reject` per
  spec Requirement 1.
- Run `dotnet test` → green.

## Step 3 — Application: `ICurrentUserAccessor.Role` + JWT claim

- `api/AgroMesh.Application/Interfaces/ICurrentUserAccessor.cs` — add `UserRole Role { get; }` and
  `bool IsAdmin { get; }` (default-interface-method `=> Role == UserRole.Admin` so every
  implementer — including test fakes — gets it for free unless they override it).
- `api/AgroMesh.Infrastructure/Services/Auth/JwtService.cs` — add
  `new Claim("role", user.Role.ToString())` alongside the existing `companyId` claim.
- `api/AgroMesh.Infrastructure/Services/Auth/CurrentUserAccessor.cs` — add
  `Role => Enum.TryParse<UserRole>(...FindFirst("role")?.Value, out var r) ? r : UserRole.Member`
  (fail closed — mirrors the `CompanyId` getter's null-coalescing pattern).
  No dedicated automated test here (`AgroMesh.Infrastructure` isn't referenced by `AgroMesh.Tests`,
  same existing boundary as `CurrentUserAccessor.CompanyId` today) — proven indirectly via the fake
  accessor used in Step 6's handler tests, which implements the same interface contract.
- `dotnet build` for the whole solution (Infrastructure/API compile against the interface change).

## Step 4 — Application/API: `AuthUserDto`/`AuthUser` gain `Role`

- `api/AgroMesh.Application/DTOs/AuthDtos.cs` — `AuthUserDto` gains `string Role`.
- `api/AgroMesh.Application/Commands/Auth/RegisterCommand.cs` and `LoginCommand.cs` — construct
  `new AuthUserDto(..., user.Role.ToString())`.
- `web/src/types/index.ts` — `AuthUser` gains `role: 'Admin' | 'Member'`.
- `dotnet build`; `npx vue-tsc --noEmit`.

## Step 5 — Infrastructure: EF config + migration for `Users.Role`

- `api/AgroMesh.Infrastructure/Persistence/Configurations/UserConfiguration.cs` — add
  `builder.Property(u => u.Role).IsRequired().HasConversion<string>().HasMaxLength(20);` — stored as
  a string column (`"Admin"`/`"Member"`) rather than an int, so a manual `SELECT` against the DB is
  self-describing and a future added role doesn't silently reorder existing ordinals in stored data.
- `dotnet ef migrations add AddUserRole --project AgroMesh.Infrastructure --startup-project AgroMesh.API`
  from `api/`. Existing rows backfill to the column default (`'Admin'`) via `defaultValue` on the
  `AddColumn` call — acceptable since (a) there is no live user data of consequence at this stage of
  the project and (b) it's the same "sole owner becomes admin" reasoning as Requirement/Design
  decision 4.
- `dotnet ef database update` against the local Postgres (`localhost:5433`, confirmed running).
- `dotnet build` for the whole solution.

## Step 6 — Application: `StockAdjustmentRequest` repository + rewritten `AdjustInventoryStockCommand`
### (red → green, handler tests)

- **Red**: new `api/AgroMesh.Tests/StockAdjustmentApprovalTests.cs` with:
  - `FakeInventoryRepository : IInventoryRepository` (in-memory list, tracks `UpdateAsync`/
    `AddMovementAsync` call counts — mirrors `FakeSectionRepository`'s `UpdateCallCount` pattern).
  - `FakeStockAdjustmentRequestRepository : IStockAdjustmentRequestRepository` (in-memory list).
  - `FakeCurrentUserAccessor : ICurrentUserAccessor` with a constructor-settable `UserId`/`Role`
    (defaults `Role = UserRole.Admin` so existing-shape tests aren't forced to opt in explicitly).
  - Tests (AC5): `Handle_ValidRequest_CreatesPendingRequest_DoesNotChangeStock`,
    `Handle_BlankNotes_ThrowsDomainException`,
    `Handle_ItemFromDifferentFarm_ThrowsDomainException` (IDOR).
  Run `dotnet test` → fails to compile (`IStockAdjustmentRequestRepository`,
  `AdjustInventoryStockCommand`'s new shape don't exist yet).
- **Green**:
  - `api/AgroMesh.Application/Interfaces/IStockAdjustmentRequestRepository.cs` — `GetByIdAsync`,
    `GetByFarmIdAsync(farmId, status?)`, `AddAsync`, `UpdateAsync`.
  - Rewrite `api/AgroMesh.Application/Commands/Inventory/AdjustInventoryStockCommand.cs`:
    `record AdjustInventoryStockCommand(string FarmId, string InventoryItemId, decimal NewQuantity,
    string? Notes) : IRequest<StockAdjustmentRequestDto>`. Handler validates blank `Notes`, resolves
    + IDOR-checks the item, reads `_currentUser.UserId`, calls `StockAdjustmentRequest.Create(...)`,
    persists via the new repo, returns the DTO.
  - `api/AgroMesh.Application/DTOs/InventoryDtos.cs` — add `StockAdjustmentRequestDto` (see spec
    Design decision 3 for the string `Status` field).
- Run `dotnet test` → green.

## Step 7 — Application: Approve/Reject commands + query (red → green, handler tests)

- **Red**: add to `StockAdjustmentApprovalTests.cs`:
  - `Approve_AdminRole_AppliesStockAndMarksApproved` (AC6)
  - `Approve_MemberRole_ThrowsForbiddenException_NoSideEffects` (AC6 — asserts
    `FakeInventoryRepository.UpdateCallCount == 0` and the request stays `Pending`)
  - `Approve_AlreadyReviewedRequest_ThrowsDomainException_EvenForAdmin` (AC8)
  - `Reject_AdminRole_MarksRejected_NoStockOrMovementSideEffects` (AC7 — asserts
    `FakeInventoryRepository.UpdateCallCount == 0` and `AddMovementCallCount == 0`)
  - `Reject_MemberRole_ThrowsForbiddenException` (AC7)
  Run `dotnet test` → fails (no `ForbiddenException`/commands yet).
- **Green**:
  - `api/AgroMesh.Domain/Exceptions/ForbiddenException.cs` — `public class ForbiddenException :
    Exception { public ForbiddenException(string message) : base(message) {} }`.
  - `api/AgroMesh.API/Middleware/ExceptionMiddleware.cs` — new `catch (ForbiddenException ex)` block
    before the generic `catch (Exception ex)`, mapping to `403`/`application/problem+json` (mirrors
    the existing `DomainException`/`NotFoundException` blocks).
  - New `api/AgroMesh.Application/Commands/Inventory/ApproveStockAdjustmentCommand.cs` and
    `RejectStockAdjustmentCommand.cs` per spec Requirements 6/7 (role check first, then farm-scope +
    `Pending` check, then the domain call).
  - New `api/AgroMesh.Application/Queries/Inventory/GetStockAdjustmentRequestsQuery.cs` — lists via
    the repo, joins in-memory against `IInventoryRepository.GetByFarmIdAsync(farmId)` for
    name/unit (spec Design decision 7 — avoids N+1).
- Run `dotnet test` → green.

## Step 8 — Infrastructure: `StockAdjustmentRequest` EF config + repository + migration

- `api/AgroMesh.Infrastructure/Persistence/Configurations/StockAdjustmentRequestConfiguration.cs` —
  mirrors `InventoryCategoryConfiguration.cs`: `HasKey`, required/maxlength props, `Status` stored
  `HasConversion<string>()` (same reasoning as `User.Role`), indexes on `FarmId` and
  `(FarmId, Status)` (the queue query's actual access pattern).
  `AgroMeshDbContext.cs` — add `DbSet<StockAdjustmentRequest> StockAdjustmentRequests`.
  `api/AgroMesh.Infrastructure/Persistence/Repositories/StockAdjustmentRequestRepository.cs` —
  mirrors `InventoryCategoryRepository.cs`.
  `api/AgroMesh.Infrastructure/DependencyInjection.cs` — register
  `IStockAdjustmentRequestRepository`.
- `dotnet ef migrations add AddStockAdjustmentRequests --project AgroMesh.Infrastructure
  --startup-project AgroMesh.API`. Review generated `Up`/`Down` — single `CreateTable` + indexes, no
  data migration needed (brand-new table).
- `dotnet ef database update` against local Postgres.
- `dotnet build` for the whole solution.

## Step 9 — API: wire endpoints

- `api/AgroMesh.API/Controllers/InventoryController.cs` — rewrite the `Adjust` action:
  `[Authorize]`, sends `new AdjustInventoryStockCommand(farmId, itemId, request.NewQuantity,
  request.Notes)`, returns `Created($"api/farms/{farmId}/inventory-adjustments/{result.Id}",
  result)` (201, per spec Design decision 1).
- New `api/AgroMesh.API/Controllers/InventoryAdjustmentsController.cs` — `[Authorize]` at the
  controller level, route `api/farms/{farmId}/inventory-adjustments`:
  - `GET /` (`?status=`) → `GetStockAdjustmentRequestsQuery`
  - `PATCH /{requestId}/approve` (body: `ReviewStockAdjustmentRequest(string? ReviewNotes)`) →
    `ApproveStockAdjustmentCommand`
  - `PATCH /{requestId}/reject` (same body shape) → `RejectStockAdjustmentCommand`
- `dotnet build`; `dotnet test` full green.

## Step 10 — Manual verification against the live local API (COMPLETE)

- Rebuilt/restarted the `agromesh_api` Docker container (`docker compose build api && docker compose
  up -d api`) so it picked up the new code + both migrations.
- **Bug found and fixed during this step**: the first submit attempt failed with 422 "Could not
  identify the requesting user." — `ICurrentUserAccessor.UserId` resolved to `null` despite a valid
  bearer token. Root cause: `AddJwtBearer` in `Program.cs` never set `MapInboundClaims = false`, so
  ASP.NET Core's default inbound claim-type mapping silently rewrote the `sub` claim to the long
  `ClaimTypes.NameIdentifier` URI, breaking every `FindFirst(JwtRegisteredClaimNames.Sub)`/`"role"`
  lookup in `CurrentUserAccessor` for **every** authenticated request in the app, not just this
  phase's new endpoints (pre-existing, silently tolerated elsewhere — e.g.
  `SubmitBoundaryPointsCommandHandler`'s ownership check is skipped, not failed, when `CompanyId` is
  null). Fixed with a one-line `opts.MapInboundClaims = false;` in `Program.cs` — necessary for
  Requirement 3/5 (identity resolution, role gate) to function at all, so treated as in-scope rather
  than deferred.
- `curl` sequence run against a freshly-registered user/farm (all responses captured verbatim):
  (a) submit an adjustment → **201**, `"status":"Pending"`, re-`GET` on the item confirms
  `currentStock` unchanged; (b) approve as the (default-Admin) token → **200**,
  `"status":"Approved"`, `currentStock` now `85` (from `100`), `GET .../movements` shows a new
  `Adjustment` (`type:2`) row with `"quantity":-15` (the pre-mutation delta, not zero); (c) reject a
  second pending request → **200**, `"status":"Rejected"`, item stock still `85` (untouched); the
  queue (`?status=Pending`) is empty after both are reviewed; (d) re-approve the already-`Approved`
  request → **422** `"This stock adjustment request has already been approved."` (not 403 — proves
  AC8's ordering: the role check passes for this admin, the Pending guard is what fires); (e) no
  `Authorization` header → **401**; (f) flipped the same test user to `Role='Member'` directly via
  SQL (`UPDATE "Users" SET "Role"='Member' ...`) and re-logged-in for a fresh Member-role token —
  confirmed a Member **can still submit** an adjustment (201, no gate on submit) but gets **403**
  `"Only an admin can approve/reject..."` on both approve and reject, with the item's stock
  independently re-confirmed unchanged after both blocked attempts. Restored the user to `Admin`
  afterward. (g) blank `notes` → 422 "A reason is required..."; (h) an item id addressed via a
  different farm in the route → 422 "not found" (IDOR guard).
- This is manual/live verification, not claimed as part of the automated test count.

## Step 11 — Frontend: types, api client, store actions

- `web/src/types/index.ts` — new `StockAdjustmentRequest` interface (mirrors
  `StockAdjustmentRequestDto`), `StockAdjustmentStatus` string union.
- `web/src/services/api.ts` — new `inventoryAdjustmentsApi`: `getAll(status?)`, `approve(id, body)`,
  `reject(id, body)`. `inventoryApi.adjust` unchanged (same endpoint, new response shape consumed by
  the store, not the client method itself).
- `web/src/stores/agro.ts`:
  - `pendingAdjustments` state (`StockAdjustmentRequest[]`).
  - `pendingAdjustmentsCount` computed (mirrors `lowStockCount`/`pendingReviews` shape).
  - `fetchPendingAdjustments()` — `inventoryAdjustmentsApi.getAll('Pending')` →
    `pendingAdjustments.value = res.data`.
  - `submitStockAdjustment(itemId, body)` — replaces the body of `adjustStock` (keep the exported
    name `adjustStock` so `InventoryView.vue`'s existing call site doesn't need a rename beyond
    what Step 12 already touches) — calls `inventoryApi.adjust`, does **not** call
    `fetchInventory()` afterward (nothing changed yet), returns the created
    `StockAdjustmentRequestDto` so the view can show its `Status`/id if useful. Also calls
    `fetchPendingAdjustments()` so the nav badge updates immediately.
  - `approveStockAdjustment(requestId, reviewNotes?)` — `inventoryAdjustmentsApi.approve` →
    `fetchPendingAdjustments()` + `fetchInventory()` (stock changed).
  - `rejectStockAdjustment(requestId, reviewNotes?)` — `inventoryAdjustmentsApi.reject` →
    `fetchPendingAdjustments()` only (no stock change).

## Step 12 — Frontend: `InventoryView.vue` Adjust modal rewrite (red → green via e2e)

- **Red**: extend `web/e2e/app.spec.ts`'s Inventory block with the new
  `// ── Inventory: Stock Adjustment Approval (Phase 3) ──` sub-section (AC10 cases). Run filtered →
  fail (modal still shows old messaging/behavior).
- **Green**:
  - `submitAdjust()` calls `store.submitStockAdjustment(...)`, shows a success state ("Adjustment
    submitted for approval") instead of silently closing, using the same `.error-msg`-adjacent inline
    pattern (new `.success-msg` style, or reuse `.warn-msg`'s visual language) — modal closes after a
    brief confirmation OR immediately with a toast-less inline note then close; simplest: close the
    modal and rely on the existing pattern of "no optimistic stock change" being provable by the
    *absence* of a stock-value change in the row, per AC10. On failure: `adjustError =
    extractErrorMessage(e, ...)` shown via `.error-msg` (previously only `console.error`).
  - No call to `fetchInventory()` after a successful submit (nothing changed).
- Run the new e2e sub-section → green. Re-run the three existing "adjust modal" tests → confirm still
  green (AC10's regression clause).

## Step 13 — Frontend: Approvals queue view + route + nav badge (red → green via e2e)

- **Red**: extend the same new e2e sub-section with the queue/approve/reject/badge cases (AC11-AC14).
  Run → fail (no view/route/nav item yet).
- **Green**:
  - New `web/src/views/InventoryApprovalsView.vue` — KPI row (pending count), loading/empty states,
    row list (item name, requested quantity vs. current stock, requester notes, Approve/Reject
    buttons gated on `store.currentUser?.role === 'Admin'`), calls
    `store.fetchPendingAdjustments()` on mount.
  - `web/src/router/index.ts` — new route `{ path: '/inventory-approvals', name:
    'inventory-approvals', component: InventoryApprovalsView, meta: { title: 'Stock Approvals', sub:
    'PENDING ADJUSTMENTS', feature: 'inventory' } }` (reuses the existing `inventory` feature flag —
    no new flag invented for a sub-page of the same feature).
  - `web/src/components/layout/Sidebar.vue` — new `ALL_NAV` entry (`id: 'inventory-approvals'`,
    `feature: 'inventory'`, `badgeFn: () => store.pendingAdjustmentsCount > 0 ?
    store.pendingAdjustmentsCount : null`).
- Run the full new sub-section → green. Run the entire Inventory block (Phases 1+2+3) to confirm no
  regressions.

## Step 14 — Full regression pass + self-review

- `dotnet test` (whole solution).
- `npx vue-tsc --noEmit`.
- `npx playwright test` (`app.spec.ts` + `gps-boundary.spec.ts`) — diff pass/fail counts against the
  pre-change baseline captured at the start of this phase.
- Self-review against correctness/tests/security/performance/consistency/regressions (see spec Test
  plan + top-level review checklist) — particularly: does Approve really compute the movement delta
  *before* calling `AdjustStock` (else the delta would always be zero)? Does Reject genuinely touch
  no inventory repo methods? Is the 403 vs 422 split actually exercised by a test for both branches?
  Is the old-token-fails-closed behavior at least indirectly provable?

## Data model / migration summary

| Entity | Change | Migration |
|---|---|---|
| `User` | + `Role` (string-backed enum, required, default `Admin`) | `AddUserRole` |
| `StockAdjustmentRequest` | new table | `AddStockAdjustmentRequests` |

## Risks / dependencies

- **Local Postgres availability** for migration-apply + manual curl steps — confirmed running
  (`agromesh_db` healthy on `localhost:5433`). If unavailable mid-build, migrations are still
  generated/reviewed (build/test don't require a live DB) and Step 10 is reported as
  blocked/skipped rather than faked.
- **`dotnet ef migrations add` against a top-level-statement `Program.cs`** — this repo's existing
  migrations (`AddInventoryCategories` etc.) prove this already works in this project; no special
  `IDesignTimeDbContextFactory` needed.
- **Ordering bug risk in Approve**: computing `delta` after calling `AdjustStock` would silently zero
  every movement's recorded quantity (since `CurrentStock` is already the new value by then) — this
  is exactly the mistake the original `AdjustInventoryStockCommand` avoided (delta computed first)
  and it's explicitly called out as a review checkpoint in Step 14.
- **Fake accessor default role**: defaulting the shared `FakeCurrentUserAccessor` to `Admin` (Step 6)
  means every *other* existing/new handler test that constructs it without specifying a role
  implicitly runs as an admin — acceptable since role only matters to the two new
  Approve/Reject handlers, but flagged so a future handler added to this fake doesn't assume
  `Member` by accident.
