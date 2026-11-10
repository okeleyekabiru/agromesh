# Plan
1. Domain: `Utilities/SecureToken`, `Entities/Invitation`, `Entities/PasswordResetToken`, `User.SetPasswordHash`. Tests: SecureTokenTests, InvitationTests.
2. Application email: `IEmailSender`, `EmailMessage`, `IAppUrls`, `EmailTemplates` (static). Tests: EmailTemplateTests.
3. Application repos `IInvitationRepository`, `IPasswordResetTokenRepository`; handlers: CreateInvitation, ListInvitations, Resend, Revoke, Preview, Accept; ForgotPassword, ResetPassword; welcome in Register. Tests with fakes.
4. Infrastructure: EF configs, DbSets, repos, migration `AddInvitationsAndPasswordReset` (generated, NOT applied), `QueuedEmailSender` + `EmailDispatchService` + `ResendEmailTransport`, `AppUrls`, DI. Tests: EmailSenderTests (stub handler).
5. API: `InvitationsController`, AuthController forgot/reset, rate limiter and forwarded headers in Program.cs, appsettings keys. Test: controller attribute tests.
6. Web: api helpers, public routes, AcceptInviteView, ForgotPasswordView, ResetPasswordView, TeamView invites, login link. vitest.
7. render.yaml (prod + dev), local setup docs, full runs, self-review.

Migration: two new tables `Invitations`, `PasswordResetTokens` (unique index on TokenHash).
Risks: rate limiter behind a proxy needs forwarded headers; in-memory queue drops on restart.
