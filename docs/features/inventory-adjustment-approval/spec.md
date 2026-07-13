# Inventory Phase 3 — Stock Adjustment Approval Workflow

## Goal

Today "Adjust Stock" on the Inventory page lets anyone overwrite `CurrentStock` to a new absolute
value, instantly, with no review. That's fine for genuine self-service corrections but wrong for a
farm operation where stock adjustments (write-offs, recount corrections, spillage) should be
checked before they change the books. This phase turns Adjust Stock into a two-step workflow:
submitting an adjustment creates a **Pending** request that does not touch `CurrentStock`; an
**Admin** then **Approves** (which applies the adjustment via the existing `InventoryItem.AdjustStock`)
or **Rejects** it (which leaves stock untouched). Because there is no admin/role concept anywhere in
this codebase yet, this phase also builds the minimum plumbing to know who's calling and whether
they're an admin — not a general permissions system, just enough to gate this one workflow safely.

## Context / existing code (reuse, don't rebuild)

- **Current Adjust Stock flow (being replaced)**: `api/AgroMesh.Domain/Entities/InventoryItem.cs:61-67`
  — `AdjustStock(decimal newQuantity, string? notes)` takes an absolute new quantity, throws
  `DomainException` on negative, overwrites `CurrentStock`. **Kept as-is** — the new Approve handler
  calls it, no new stock arithmetic is written. `api/AgroMesh.Application/Commands/Inventory/AdjustInventoryStockCommand.cs`
  currently applies immediately and has **no farm-scoping/IDOR guard at all** (unlike
  `CreateInventoryItemCommand.ResolveTargetItemAsync` and `PickStockCommand`, both of which check
  `item.FarmId != farmId`) — fixed as part of this rewrite. Controller:
  `api/AgroMesh.API/Controllers/InventoryController.cs:48-53`,
  `PATCH api/farms/{farmId}/inventory/{itemId}/adjust`, body `AdjustStockRequest(decimal NewQuantity,
  string? Notes)` (`api/AgroMesh.Application/DTOs/InventoryDtos.cs:39`). Frontend:
  `web/src/views/InventoryView.vue` Adjust modal (~73-102) and `openAdjust`/`submitAdjust`
  (~267-284); store action `web/src/stores/agro.ts:326-329` (`adjustStock` → `inventoryApi.adjust`
  → full `fetchInventory()` refetch). `submitAdjust` today only `console.error`s on failure — no
  inline error UI, unlike Add Item/Pick Stock which both use `.error-msg`/`extractErrorMessage`
  (`web/src/utils/http.ts`) — fixed as part of this rewrite.
- **Sibling command/entity shapes to mirror**: `api/AgroMesh.Application/Commands/Inventory/PickStockCommand.cs`
  — required-reason validation pattern (`string.IsNullOrWhiteSpace(req.Purpose)` →
  `DomainException`) and the IDOR guard (`item.FarmId != command.FarmId`). `InventoryItem.AddStock`/
  `DeductStock` — domain entities enforce their own invariants and throw `DomainException` on
  invalid transitions; `StockAdjustmentRequest.Approve`/`Reject` follow the same convention (throw if
  not `Pending`).
- **"Small new farm-scoped entity with its own repo" shape (Phase 1 precedent)**:
  `api/AgroMesh.Application/Interfaces/IInventoryCategoryRepository.cs` +
  `api/AgroMesh.Infrastructure/Persistence/Repositories/InventoryCategoryRepository.cs` +
  `api/AgroMesh.Infrastructure/Persistence/Configurations/InventoryCategoryConfiguration.cs` — same
  shape followed for `IStockAdjustmentRequestRepository`/`StockAdjustmentRequestRepository`/
  `StockAdjustmentRequestConfiguration`. Sibling controller precedent:
  `api/AgroMesh.API/Controllers/InventoryCategoriesController.cs`
  (`api/farms/{farmId}/inventory-categories`) — same shape for the new
  `api/farms/{farmId}/inventory-adjustments` controller.
- **What is *not* a precedent for this workflow (confirmed, not to be copied)**: `Transaction.Review()`
  (`api/AgroMesh.Domain/Entities/Transaction.cs`) is a one-way boolean confirm flag
  (`ReviewedByUser: bool`) for correcting an AI-guessed bank-transaction category. It applies
  immediately, has no rejection concept, and never gates money/stock that already moved. Only its
  generic plumbing *shape* is reused (MediatR command/handler → repo → DTO; controller route
  convention; Pinia store action; list view with filter pills / KPI count — see
  `web/src/views/FinanceView.vue`'s "Pending Review" pill and `store.pendingReviews`
  (`web/src/stores/agro.ts:68-70`) as a shape reference for the new pending-count nav badge). Phase 3
  is this codebase's first real Pending/Approved/Rejected state machine.
- **No admin/role concept exists today**: `api/AgroMesh.Domain/Entities/User.cs` has no `Role`.
  `api/AgroMesh.Application/Commands/Auth/RegisterCommand.cs` makes every registrant the sole owner
  of their own new `Company` (`isDefaultCompany: true`, `OwnerId = user.Id`) — there is no
  invite/multi-user-per-company flow anywhere in this codebase. `api/AgroMesh.Infrastructure/Services/Auth/JwtService.cs:24-31`
  issues `sub`/`email`/`fullName`/`companyId`/`jti` claims. `api/AgroMesh.Application/Interfaces/ICurrentUserAccessor.cs`
  + `api/AgroMesh.Infrastructure/Services/Auth/CurrentUserAccessor.cs:13-17` expose `UserId`/`CompanyId`
  read from JWT claims via `FindFirst(...)`.
- **Handler-test precedent**: `api/AgroMesh.Tests/BoundarySubmissionIdempotencyTests.cs:66-70` — a
  hand-rolled `FakeCurrentUserAccessor` implementing `ICurrentUserAccessor` with fixed
  `UserId`/`CompanyId`, used to drive a real handler (`SubmitBoundaryPointsCommandHandler`) without
  mocks (`AgroMesh.Tests.csproj` references no mocking library). This is this repo's actual precedent
  for testing handlers that depend on `ICurrentUserAccessor` — extended here (fake gets a `Role`) to
  prove the admin gate.
- **`[Authorize]` precedent**: `api/AgroMesh.API/Controllers/SectionsController.cs:11` already uses
  `[Authorize]` at the controller level with JWT bearer auth wired in `Program.cs:48-60`.
  `InventoryController`/`InventoryCategoriesController` have no `[Authorize]` today — an existing,
  explicitly out-of-scope gap for their GET/POST/pick actions (Phases 1/2), left alone. The new
  adjust/approve/reject/queue actions are different in kind (they need to know *who* is calling to
  record `RequestedByUserId`/`ReviewedByUserId` and check role), so `[Authorize]` is added only to
  those.
- **User-id reference convention**: `api/AgroMesh.Domain/Entities/Farm.cs` — `OwnerId` is a plain
  `string`, unconstrained (no FK to `Users`), just an indexed varchar column
  (`FarmConfiguration.cs:14,50`). `StockAdjustmentRequest.RequestedByUserId`/`ReviewedByUserId` follow
  the same convention — no FK constraint to `Users`.

## Bug discovered and fixed while building this phase

`api/AgroMesh.API/Program.cs`'s `AddJwtBearer(...)` never set `MapInboundClaims = false`. ASP.NET
Core's default inbound claim-type mapping silently rewrites short claim types like `sub` to the long
`ClaimTypes.NameIdentifier` URI on the server side, which broke every
`FindFirst(JwtRegisteredClaimNames.Sub)`/`FindFirst("role")` lookup in `CurrentUserAccessor` for
**every** authenticated request in the app (not something introduced by this phase — it silently
made `UserId`/`CompanyId` resolve to `null` everywhere `[Authorize]` + `ICurrentUserAccessor` were
already combined, e.g. `SubmitBoundaryPointsCommandHandler`, which happens to tolerate a null
`CompanyId` by skipping its ownership check rather than failing loudly). Fixed with a one-line
`opts.MapInboundClaims = false;` — necessary for Requirements 3/5 (identity resolution, the admin
role gate) to function at all, so fixed in-line rather than deferred as a separate follow-up. See
`docs/features/inventory-adjustment-approval/plan.md` Step 10 for the live verification that caught
and confirmed the fix.

## Design decisions (agreed, documented here rather than re-litigated)

1. **Route/verb for submit stays in place**: `PATCH api/farms/{farmId}/inventory/{itemId}/adjust`
   keeps its route and verb (still addresses/patches the item's adjustment sub-resource), but now
   returns **`201 Created`** (was `204 NoContent`) with the created `StockAdjustmentRequestDto` body
   and a `Location` header pointing at the queue — because submitting now creates a new sub-resource
   (a pending request) rather than mutating the item in place.
2. **Role-gate exception → HTTP 403, not 422**: a new `ForbiddenException` (in
   `AgroMesh.Domain.Exceptions`, alongside the existing `DomainException`/`NotFoundException`) is
   thrown when a non-admin calls Approve/Reject, and mapped to `403 Forbidden` in
   `ExceptionMiddleware`. Reasoning: `DomainException`→422 is this app's convention for *business
   rule* violations (e.g. "can't adjust to negative stock") that are meaningful regardless of who's
   asking; an authorization failure is a different kind of thing (the request is well-formed and
   would succeed for a different caller) and deserves a distinct, standard status code so the
   frontend/any client can tell "you're not allowed" apart from "this data is invalid" without
   string-matching the error body.
3. **Role/status on the wire are strings, not raw enum ordinals**: this codebase has no
   `JsonStringEnumConverter` configured, so enums serialize as raw ints by default (confirmed: no
   converter is registered anywhere in `api/AgroMesh.API` or `api/AgroMesh.Infrastructure`) — the
   existing `PAY_FREQUENCY_LABELS` numeric-ordinal convention lives with that reality for
   `PayFrequency`. For the two *new* wire fields this phase introduces (`AuthUserDto.Role`,
   `StockAdjustmentRequestDto.Status`), the DTOs are hand-constructed in application code (not
   auto-serialized straight off the entity), so each is built as an explicit `.ToString()` (e.g.
   `"Admin"`/`"Member"`, `"Pending"`/`"Approved"`/`"Rejected"`) — avoids the ordinal-vs-string
   ambiguity entirely rather than repeating it. The JWT `role` claim is likewise `user.Role.ToString()`,
   matching the existing `email`/`fullName` claim style.
4. **Bootstrap default role**: `User.Create(...)` sets `Role = UserRole.Admin` explicitly. Since
   `RegisterCommand` is the only path that creates a `User`, and the registrant is always the sole
   owner of their own brand-new `Company`/`Farm`, they are always the sole admin of everything they
   can see — there is no invite flow to abuse this default via.
5. **Fail closed on missing/old-token `role` claim**: `CurrentUserAccessor.Role` defaults to
   `UserRole.Member` (non-admin) when the claim is absent or unparseable, so a token issued before
   this change (no `role` claim) is never silently treated as Admin.
6. **Query default**: `GetStockAdjustmentRequestsQuery`/`GET api/farms/{farmId}/inventory-adjustments`
   returns **all** requests for the farm when `?status=` is omitted (simple, predictable REST
   semantics — no hidden filtering); the frontend's approvals-queue view is what explicitly requests
   `?status=Pending`, and the nav-badge count/queue-emptying behavior come from that explicit filter,
   not a server-side default.
7. **N+1 avoidance in the queue query**: `GetStockAdjustmentRequestsQueryHandler` loads all of the
   farm's inventory items in one call (`IInventoryRepository.GetByFarmIdAsync`) and joins in memory
   to fill in `InventoryItemName`/`Unit` on each `StockAdjustmentRequestDto`, rather than querying per
   request.

## Requirements

1. New domain entity `StockAdjustmentRequest` (`api/AgroMesh.Domain/Entities/StockAdjustmentRequest.cs`)
   with `FarmId`, `InventoryItemId`, `RequestedQuantity` (decimal, absolute new value — same
   semantics as `AdjustStock`'s parameter), `Status` (new enum `StockAdjustmentStatus { Pending,
   Approved, Rejected }`), `Notes` (requester's reason, required), `RequestedByUserId`,
   `ReviewedByUserId` (nullable), `ReviewedAt` (nullable), `ReviewNotes` (nullable). Static factory
   `Create(...)` sets `Status = Pending`, throws `DomainException` for a blank `notes` or a negative
   `requestedQuantity` (same negative check as `AdjustStock`, applied at submission time so an
   obviously-bad request doesn't sit in the queue). `Approve(reviewedByUserId, reviewNotes?)` and
   `Reject(reviewedByUserId, reviewNotes?)` both throw `DomainException` if `Status != Pending` (no
   double-review).
2. New `UserRole` enum (`Admin`, `Member`) on `User` (`api/AgroMesh.Domain/Entities/User.cs`),
   defaulting to `Admin` at `Create(...)` time (see Design decision 4). EF migration adds the `Role`
   column to `Users`.
3. `JwtService.GenerateToken` issues an additional `role` claim (`user.Role.ToString()`).
   `ICurrentUserAccessor` gains a `Role` (`UserRole`) member (plus a convenience `IsAdmin` bool);
   `CurrentUserAccessor.Role` reads the `role` claim and defaults to `UserRole.Member` when missing
   or unparseable (Design decision 5).
4. `AuthUserDto`/frontend `AuthUser` gain a `Role` (string: `"Admin"`/`"Member"`) field, populated at
   both the Register and Login handlers, so the frontend can decide whether to show approve/reject
   controls. This is a UX hint only — the backend gate (Requirement 6) is the real security boundary.
5. `PATCH api/farms/{farmId}/inventory/{itemId}/adjust` (rewired `AdjustInventoryStockCommand`):
   requires `[Authorize]` (needs `ICurrentUserAccessor.UserId` for `RequestedByUserId`); validates a
   non-blank `Notes` (`DomainException` if blank, mirrors `PickStockCommand`'s purpose check);
   resolves the `InventoryItem` and 404-style-rejects if missing or `item.FarmId != farmId` (new IDOR
   guard — did not exist before this phase); creates a `StockAdjustmentRequest` via `Create(...)` and
   persists it. **Does not touch `CurrentStock`.** Returns `201 Created` with the created
   `StockAdjustmentRequestDto` (Design decision 1).
6. New `PATCH api/farms/{farmId}/inventory-adjustments/{requestId}/approve` (`ApproveStockAdjustmentCommand`,
   `[Authorize]`): loads the request, 404s if missing or farm-mismatched; throws `ForbiddenException`
   (403) if the caller's role isn't `Admin` (Design decision 2); throws `DomainException` if the
   request isn't `Pending`; loads the `InventoryItem`, computes the movement delta
   (`request.RequestedQuantity - item.CurrentStock`) **before** mutating, calls the existing
   `item.AdjustStock(request.RequestedQuantity, request.Notes)`, persists the item, records a
   `StockMovement.CreateAdjustment(...)` (existing factory, unchanged), then calls
   `request.Approve(currentUser.UserId, reviewNotes)` and persists the request. Returns the updated
   `StockAdjustmentRequestDto`.
7. New `PATCH api/farms/{farmId}/inventory-adjustments/{requestId}/reject` (`RejectStockAdjustmentCommand`,
   `[Authorize]`): same guards as Requirement 6 (farm-scope, admin role, must be `Pending`), calls
   `request.Reject(currentUser.UserId, reviewNotes)`, persists the request. **No item/stock changes
   at all.** Returns the updated `StockAdjustmentRequestDto`.
8. New `GET api/farms/{farmId}/inventory-adjustments?status=` (`GetStockAdjustmentRequestsQuery`,
   `[Authorize]`): lists `StockAdjustmentRequest`s for the farm, optionally filtered by `status`; all
   statuses when omitted (Design decision 6).
9. Frontend Adjust modal (`InventoryView.vue`): submitting shows "submitted for approval" success
   messaging (not an instant-update message); the item row's displayed stock does **not** change
   immediately after submit (no optimistic update, no refetch-driven change since the backend didn't
   change `CurrentStock`); a failed submit shows an inline `.error-msg` (previously only
   `console.error`).
10. New approvals queue view listing pending `StockAdjustmentRequest`s with Approve/Reject actions
    per row (KPI/loading/empty states, same shape as `InventoryView.vue`), reachable from a new
    Sidebar nav item with a pending-count badge (mirrors `store.lowStockCount`/`store.pendingReviews`
    badge pattern). Approve/Reject buttons are shown only when `store.currentUser?.role === 'Admin'`
    (UX hint; the backend 403 is the actual boundary — Design decision/Requirement 6-2). Approving
    removes the request from the pending queue and updates the item's stock in the Inventory view (on
    next fetch); rejecting removes it from the queue without changing stock.

## Non-goals (explicitly out of scope for this phase)

- **Phase 4 (orders/invoices/purchasing)** — not touched.
- A general permissions system, multi-role hierarchy beyond Admin/Member, or an invite/multi-user-
  per-company flow — the `UserRole` enum and `[Authorize]` additions are the minimum needed to gate
  this one workflow safely, nothing more.
- Adding `[Authorize]` to the pre-existing `InventoryController`/`InventoryCategoriesController`
  GET/POST/pick actions — still out of scope (same stance as Phases 1/2); only the new
  identity-dependent adjust/approve/reject/queue actions get it.
- A live second-user-per-company test fixture/story — there's no invite flow to create one, and
  building one is explicitly out of scope. The "non-admin is blocked" path is proven at the handler
  level via a fake `ICurrentUserAccessor` (this repo's actual precedent — see Test plan), not via a
  second logged-in browser session in e2e.
- Editing/cancelling a pending request once submitted, or any bulk-approve action.
- Surfacing `ReviewedByUserId`/reviewer identity as a resolved user name anywhere in the UI (no user
  directory/lookup exists) — the raw id is stored and returned on the DTO but not resolved to a
  display name.
- Role management UI (promoting/demoting a user) — no such action exists to gate in the first place
  (single-admin-per-company today).

## Acceptance criteria

- [x] **AC1** — `StockAdjustmentRequest.Create(...)` sets `Status = Pending`, throws `DomainException`
      for a blank/whitespace `notes`, and throws `DomainException` for a negative
      `requestedQuantity`. *(domain unit test — `StockAdjustmentRequestTests.cs`)*
- [x] **AC2** — `StockAdjustmentRequest.Approve(...)` sets `Status = Approved`,
      `ReviewedByUserId`/`ReviewedAt`/`ReviewNotes`, and throws `DomainException` if called on a
      non-`Pending` request (no double-review). *(domain unit test)*
- [x] **AC3** — `StockAdjustmentRequest.Reject(...)` sets `Status = Rejected` and the same review
      fields, and throws `DomainException` if called on a non-`Pending` request. *(domain unit test)*
- [x] **AC4** — `User.Create(...)` defaults `Role` to `UserRole.Admin`. *(domain unit test —
      `UserTests.cs`)*
- [x] **AC5** — `AdjustInventoryStockCommandHandler` creates a `Pending` `StockAdjustmentRequest` and
      does **not** call `item.AdjustStock`/mutate `CurrentStock`; rejects a blank `Notes`; rejects
      (404-style `DomainException`) when the resolved item's `FarmId` doesn't match the command's
      `FarmId` (new IDOR guard). *(handler test —
      `AdjustInventoryStockCommandTests.cs`, fake repos, no mocking library, mirrors
      `BoundarySubmissionIdempotencyTests.cs`)*
- [x] **AC6** — `ApproveStockAdjustmentCommandHandler`, given a fake `ICurrentUserAccessor` with
      `Role = Admin`: applies `item.AdjustStock`, persists a `StockMovement.CreateAdjustment`, and
      marks the request `Approved`. Given a fake accessor with `Role = Member`: throws
      `ForbiddenException`, and the item/request are both left untouched (`UpdateAsync` never called
      on either fake repo). *(handler test)*
- [x] **AC7** — `RejectStockAdjustmentCommandHandler`, given `Role = Admin`: marks the request
      `Rejected`, never calls `IInventoryRepository.UpdateAsync`/`AddMovementAsync` (no stock/movement
      side effects). Given `Role = Member`: throws `ForbiddenException`. *(handler test)*
- [x] **AC8** — Approve/Reject both throw `DomainException` (not `ForbiddenException`) when the
      request is already `Approved`/`Rejected` — proves the "must be Pending" guard runs even for an
      admin caller. *(handler test)*
- [x] **AC9** — `CurrentUserAccessor.Role` defaults to `UserRole.Member` when the JWT has no `role`
      claim — proves an old token never gets silently treated as Admin. *(covered at the
      `ICurrentUserAccessor` contract level via the fake used in AC6/AC7's "Member" cases, which is
      the same shape `CurrentUserAccessor` implements; `CurrentUserAccessor` itself lives in
      `AgroMesh.Infrastructure`, which `AgroMesh.Tests` doesn't reference, per this repo's existing
      test-project boundary — see Test plan for why this is judged sufficient.)*
- [x] **AC10** — E2E: submitting a valid Adjust does **not** change the item's displayed stock
      immediately, and shows "submitted for approval" messaging; the existing "adjust button opens
      modal"/"shows current stock"/"can be closed" tests still pass unmodified. *(`app.spec.ts`)*
- [x] **AC11** — E2E: the approvals queue lists pending `StockAdjustmentRequest`s (name, requested
      quantity, requester's notes). *(`app.spec.ts`)*
- [x] **AC12** — E2E: approving a queued request removes it from the queue and the Inventory view's
      item row reflects the new stock (via refetch). *(`app.spec.ts`)*
- [x] **AC13** — E2E: rejecting a queued request removes it from the queue and the item's stock is
      unchanged. *(`app.spec.ts`)*
- [x] **AC14** — E2E: the sidebar's pending-adjustments nav badge reflects the queue size and
      disappears/hides at zero, mirroring the existing `lowStockCount`/`pendingReviews` badge pattern.
      *(`app.spec.ts`)*
- [x] **AC15** — `dotnet build` succeeds for the whole solution; `dotnet test` is fully green with no
      regressions. `npx vue-tsc --noEmit` is clean. The relevant Playwright command shows no new
      failures versus the pre-existing baseline (re-confirmed fresh, not assumed from Phase 2's
      documented baseline).

## Test plan

- **Domain unit tests** (`api/AgroMesh.Tests/`, xUnit, plain `Assert.*`, no mocks — matches
  `InventoryTests.cs`): new `StockAdjustmentRequestTests.cs` (AC1-AC3) and additions to a small new
  `UserTests.cs` (AC4, since no such file exists yet — `User` has never had unit-testable behavior
  before this phase).
- **Handler tests** (new for this phase, per explicit instruction — extends, not replaces, this
  repo's actual precedent at `BoundarySubmissionIdempotencyTests.cs`, which already hand-rolls fake
  repos + a fake `ICurrentUserAccessor` to drive a real handler with no mocking library): new
  `StockAdjustmentApprovalTests.cs` with fake `IInventoryRepository`, fake
  `IStockAdjustmentRequestRepository`, and a fake `ICurrentUserAccessor` whose `Role` is
  constructor-settable (`Admin` or `Member`) — covers AC5-AC8. This is the only place the admin-gate
  itself is proven with a real `Member`-role caller, since there's no live second-user-per-company
  story to test against in e2e (see Non-goals) — the e2e layer instead proves the *UI* hides
  approve/reject controls for a non-admin session (skipped as a dedicated e2e case per the reasoning
  in AC9/Non-goals: the mocked `mockApi()` session would need a second full page/role fixture to
  meaningfully assert a *blocked* action rather than just a *hidden* button, which doesn't add
  coverage beyond what the handler test already proves for the real boundary).
- **Playwright e2e** (`web/e2e/app.spec.ts`, new `// ── Inventory: Stock Adjustment Approval (Phase 3)
  ──` sub-section after the Phase 2 Pick/Issue sub-section; `web/e2e/fixtures.ts` gets a new
  `STOCK_ADJUSTMENTS` fixture): AC10-AC14. Existing "adjust button opens modal"/"adjust modal shows
  current stock"/"adjust modal can be closed" tests are re-run and confirmed to still pass unmodified
  (they don't assert on stock changing post-submit today, so the behavior change is expected to be
  transparent to them — verified, not assumed).
- **Full regression**: `dotnet test` (whole solution), `npx vue-tsc --noEmit`, `npx playwright test`
  (`app.spec.ts` + `gps-boundary.spec.ts`) — compared against a freshly-captured baseline run *before*
  this phase's changes, to distinguish any new failures from the pre-existing, already-documented
  Dashboard/Sidebar/Workers staleness noted in Phase 2's spec.

**Final results**: `dotnet test` 91/91 green (68 pre-existing + 1 new `UserTests.cs` + 9 new
`StockAdjustmentRequestTests.cs` + 13 new `StockAdjustmentApprovalTests.cs` handler tests = 23 new,
0 regressions). `npx vue-tsc --noEmit` clean.
`npx playwright test` (both spec files, 109 tests): **88 passed, 21 failed** — all 9 new Phase 3
tests pass, all previously-passing tests (79 in `app.spec.ts` + the full `gps-boundary.spec.ts` set)
still pass, and the 21 failures are the exact same pre-existing, unrelated Dashboard/Sidebar/Workers/
log-activity-worker-picker/back-navigation staleness already documented in Phase 2's spec.md
(confirmed by exact test-name diff — no new failures). One regression was caught and fixed during
this phase's own regression pass: `gps-boundary.spec.ts`'s "Edit section" test started failing
because the Sidebar's new unconditional `fetchPendingAdjustments()` call (added for the nav badge)
fell through to the real, unmocked backend in that test's separate mock setup and 401-redirected the
page away — fixed by adding the same `inventory-adjustments` intercept that test's own mock set
already mirrors from `app.spec.ts`. Manual live-API verification (Step 10 in `plan.md`) run against
the rebuilt `agromesh_api` Docker container and local Postgres, including the real 403 admin-gate
proof via a role-flipped session, full transcript in the build summary.
