# Email setup (Resend)

Config keys (environment variables use double underscores): `Resend__ApiKey`, `Resend__FromAddress`, `Resend__Enabled`, `App__WebBaseUrl`.

## Local
1. Create a Resend account and an API key. For tests without a domain use the sender `Ilevora <onboarding@resend.dev>`; Resend then only delivers to your own account email.
2. Do not put the key in a tracked file. Either export variables before `docker compose up`:
   `RESEND_ENABLED=true RESEND_API_KEY=re_xxx docker compose up -d --no-deps api`
   or add `"Resend": { "ApiKey": "re_xxx", "Enabled": true }` to the untracked `api/Ilevora.API/appsettings.Development.json` (mounted into the container).
3. `App__WebBaseUrl` defaults to `http://localhost:5173`; invite and reset links point there.
4. With `Enabled=false` or no key, the API logs `Email not sent (Resend disabled or not configured): to ..., subject ...` and carries on. Tokens are never logged, so for local testing enable Resend or read the link from the email.

## Render (prod and dev services)
Set `Resend__ApiKey` (secret) and `App__WebBaseUrl` (the Vercel URL, no trailing slash) in the dashboard of each service. `Resend__FromAddress` must use a domain verified in Resend (add the DNS records for ilevora.com). Dev should use a separate Resend key.

## Outbox
Queued mail lives in the `EmailOutbox` table, so it survives restarts and works with several API instances. When Resend is disabled or has no key nothing is queued (the send is only logged).

## Database
Migrations `AddInvitationsAndPasswordReset` (tables `Invitations`, `PasswordResetTokens`) and `AddEmailOutboxAndSessionVersion` (table `EmailOutbox`, columns `Users.TokenVersion`, `Users.EmailNotificationsEnabled`). It was generated but not applied to any live database; the API applies it on startup in Development, otherwise run `dotnet ef database update` after taking a backup.
