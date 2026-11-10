# Transactional email (Resend) and invitations

## Goal
Ilevora sends branded transactional email through Resend. First use: an owner/admin (or farm manager, for supervisors) invites someone by email; the invitee opens a single-use expiring link, sets a password and joins the farm. The same infrastructure powers forgot/reset password and welcome emails. A Resend outage or missing key must never break a user action.

## Context / existing code
- `User` (`api/Ilevora.Domain/Entities/User.cs`), `UserRole`, `AddStaffUserCommandHandler.CanAdd` (role rules reused for invites), `GetTeamQuery`, `UsersController`, `AuthController`, `RegisterCommand`, `LoginCommand`, `IPasswordHasher` (BCrypt), `IJwtService`, `ICurrentUserAccessor`, `ICompanyRepository`.
- `ExceptionMiddleware` maps `DomainException` to 422, `ForbiddenException` to 403, `NotFoundException` to 404.
- Hosted-service pattern: `BackgroundJobQueue`, `SchedulingBackgroundService` (Infrastructure/Services).
- Web: `web/src/views/TeamView.vue` (currently creates users with a temporary password), `router/index.ts`, `services/api.ts`.
- Brand: `docs/brand/brand-guide.md` (green `#138A44`, Plus Jakarta Sans). The farm logo is never used.
- There was NO email service, invitation entity or password reset.

## Requirements
Infrastructure
1. `IEmailSender.SendAsync(EmailMessage)` in Application. Production impl enqueues to an (now replaced by the DB outbox, see addendum) in-memory channel and returns immediately; a hosted service delivers via Resend (`POST https://api.resend.com/emails`, bearer key) with one retry; failures are logged (no key, no token, no body) and never thrown to callers.
2. Config: `Resend:ApiKey`, `Resend:FromAddress`, `Resend:Enabled`, `App:WebBaseUrl`. If disabled or no key, the message is logged (recipient and subject only) and skipped.
3. Templates: branded HTML plus plain-text for invitation, password reset, password changed, welcome. HTML-encode all dynamic values.

Invitations
4. `POST /api/invitations` (Admin: FarmManager/Supervisor; FarmManager: Supervisor only) creates an invitation for an email and role, emails a link `{WebBaseUrl}/accept-invite?token=...`. Rejects invalid email, already-registered email, existing pending invite for the same email in the company, and unassignable roles.
5. `GET /api/invitations` lists the company's invitations with status (Pending, Accepted, Revoked, Expired); admin and farm manager only.
6. `POST /api/invitations/{id}/resend` issues a new token (old one stops working), extends expiry, re-sends; only for non-accepted, non-revoked invites of the caller's company.
7. `DELETE /api/invitations/{id}` revokes a pending invite; its link stops working.
8. `POST /api/invitations/preview` (body `{token}`) (anonymous) returns farm name, email, role for a valid token; generic 422 for invalid/expired/used/revoked.
9. `POST /api/invitations/accept` (anonymous) with token, full name, password (min 8) creates the user in the inviter's company with the invited role, marks the invite accepted (single use), returns the same `AuthResponse` as login, sends a welcome email.
10. Tokens: 32 random bytes, base64url; only a SHA-256 hash is stored; lookup by hash then fixed-time compare; expiry 7 days (invite), 1 hour (reset). Tokens are never logged.

Password reset
11. `POST /api/auth/forgot-password` always returns 200 with the same body whether or not the email exists; if an active user exists, a reset token is created and emailed. No new token if one was issued in the last 60 seconds for that user.
12. `POST /api/auth/reset-password` with token and new password (min 8) sets the password, marks the token used, voids other outstanding reset tokens for the user, sends a "password changed" email. Invalid/expired/used token gives a generic 422.
13. Rate limits (ASP.NET Core rate limiter, per client IP, honouring `X-Forwarded-For`): anonymous token endpoints 10 req / 15 min; forgot-password 5 req / 15 min; invitation create/resend 30 / hour per user.

Welcome
14. A welcome email is sent after self-registration and after accepting an invite.

Web / mobile
15. Team page: invite form (email, role), invitations list with Resend and Revoke. Public pages `/accept-invite`, `/forgot-password`, `/reset-password`; login entry point gets a "Forgot password" link.
16. Mobile: no change. Invitees accept via the web link; mobile login unchanged. (Forgot-password link in mobile is a follow-up.)

## Flow audit (wired / skipped)
| Flow | Decision | Why |
|---|---|---|
| Invitation | Wired | Core goal |
| Forgot / reset password | Wired | Missing basic flow |
| Password changed notice | Wired | Security notice after reset |
| Welcome (register, accept invite) | Wired | Cheap, expected |
| Admin adds staff with temp password (`POST /api/users`) | Skipped | Superseded by invites; email would not carry the password. API kept for compatibility, UI now uses invites |
| Account disabled/enabled notice | Skipped | Admin tells the person; low value, follow-up |
| Invoice sent to customer | Skipped | Needs PDF attachment, a "send" action and UX; there is no sent state, `BillToEmail` is only a field. Follow-up |
| Purchase order to supplier / order status | Skipped | Needs attachment and an explicit send action; status changes are made by the same staff who would be notified |
| Low-stock alert | Skipped | `IsLowStock` exists but there is no alert state, so a trigger would spam; needs a digest design |
| Stock adjustment / price change approval outcome | Skipped | Requester notification is nice but out of scope |
| Per-user email opt-out | Out of scope | Only security/transactional mail is sent |

## Non-goals
Marketing mail, bounce/webhook handling, email verification on signup, delivery persistence/outbox (queue is in memory; a restart can drop queued mail, acceptable because invites can be resent), mobile UI changes, i18n.

## Acceptance criteria
- [x] AC1 Secure token: unique, SHA-256 hashed, fixed-time verify rejects wrong tokens (SecureTokenTests)
- [x] AC2 Invitation domain: status transitions; resend rotates token and old token fails (InvitationTests)
- [x] AC3 Create invite sends one email with a link containing the raw token while only the hash is stored (CreateInvitationTests)
- [x] AC4 Create invite enforces role matrix and rejects duplicate/registered/invalid emails; nothing sent on failure (CreateInvitationTests)
- [x] AC5 List is company-scoped; supervisors forbidden (InvitationManageTests)
- [x] AC6 Resend/revoke are company-scoped; revoked link unusable; resend invalidates old link (InvitationManageTests)
- [x] AC7 Preview and accept: valid token creates user with role/company, returns auth, sends welcome; reused, expired, revoked, wrong token fail with the same generic message; short password rejected (AcceptInvitationTests)
- [x] AC8 Forgot password: unknown email identical result and no mail; known active user mailed; throttled within 60s; inactive user gets nothing (PasswordResetTests)
- [x] AC9 Reset password: hash changed, single use, expiry enforced, password-changed mail, short password rejected (PasswordResetTests)
- [x] AC10 Register sends welcome and survives a failing sender (RegisterWelcomeTests)
- [x] AC11 Sender: disabled/keyless skips without HTTP; success posts correct payload and auth header; failure swallowed (EmailSenderTests)
- [x] AC12 Templates contain link and brand green, no logo, HTML-encode names (EmailTemplateTests)
- [x] AC13 Controllers: anonymous endpoints `[AllowAnonymous]` with rate-limit attributes; management requires `[Authorize]` (InvitationControllerTests)
- [x] AC14 Web: new views and team invites render and call the API (vitest: web/src/views/emailFlows.test.ts)
- [x] AC15 `dotnet test`, `vue-tsc --noEmit`, `vitest run` green

## Test plan
Domain unit (tokens, invitation, reset token); handler tests with fake repos and `FakeEmailSender`; infrastructure tests with a stub `HttpMessageHandler` for Resend; reflection tests for controller attributes (precedent: Orders authorize tests); vitest component tests for the new views. Playwright not used (suite is red per CLAUDE.md).

## Addendum: hardening (follow-up round)
- **Outbox replaces the in-memory queue.** `IEmailSender` inserts an `EmailOutbox` row (only when Resend is enabled and configured). `EmailDispatchService` polls every 10s (1s while busy), claims due rows with one guarded `UPDATE` per row (status Pending and due, or Sending with an expired 2 minute lease), sends through Resend, and retries with backoff 1m, 5m, 15m, 1h up to 5 attempts, then marks the row Failed. Several instances are safe: only the instance whose UPDATE changed the row sends it. A crash mid-send leaves a Sending row that is reclaimed after the lease (at-least-once, so a rare duplicate is possible). Body and text are cleared when a row is Sent or Failed, so links do not linger; Sent/Failed rows are purged after 7 days. `LastError` stores only an error type. Tests: `EmailOutboxTests.cs` (entity and processor with fakes; the SQL claim itself needs Postgres and is not unit tested).
- **Sessions end on password change.** `User.TokenVersion` is bumped by `SetPasswordHash` and carried in the JWT as claim `tv`; JWT validation rejects a mismatch. Old tokens without the claim stay valid until the user's first password change.
- **Optional email opt-out.** `User.EmailNotificationsEnabled` (default true), `GET/PUT /api/users/me/email-preference`, toggle on the web Farm Settings page. Optional email so far: "your invitee joined" sent to the inviter. Invite, reset, password-changed and welcome are always sent. Mobile toggle not built.
- **Mobile.** Login screen has "Forgot password?" (opens `/forgot-password` through `openWebApp`) and a hint for invited users; `flutter analyze` shows only the 2 known `Path` warnings.
- **Tests.** The date-fragile vendor invoice test now uses dates relative to today.
- Migration `AddEmailOutboxAndSessionVersion` (table `EmailOutbox`, columns `Users.TokenVersion`, `Users.EmailNotificationsEnabled`) generated, not applied.
