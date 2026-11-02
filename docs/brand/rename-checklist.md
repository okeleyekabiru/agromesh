# Rename checklist: AgroMesh to Ilevora

Built from a repo-wide search (case-insensitive "agromesh", excluding build output and node_modules): about 2,980 occurrences in about 520 files. Nothing has been changed. Risk: **L** low, **M** medium, **H** high (can break users, data or deployments).

Rule of thumb: change what users see first; leave internal C# namespaces and identifiers for last, or never.

## A. User-visible strings (do first)

| Where | File | Risk | Notes |
|---|---|---|---|
| Browser title | `web/index.html` ("AgroMesh - Farm Operating System") | L | Also add meta description/OG tags with new name |
| Favicon | `web/public/favicon.svg` | L | Replace with new mark |
| Landing page | `web/src/views/LandingView.vue` (sign in/create account text, footer copyright) | L | Review hero copy against new voice |
| Onboarding | `web/src/views/OnboardingView.vue` (`onboard-logo` text) | L | |
| Review/confirm | `web/src/views/ReviewConfirmView.vue` (`review-logo` text) | L | |
| Dashboard fallback | `web/src/views/DashboardView.vue` (`store.farm?.name ?? 'AgroMesh'`) | L | |
| Sidebar logo | `web/src/components/layout/Sidebar.vue` (uses `AgroMeshMark`, comment) | L | Product rule: always the Ilevora logo here |
| Logo component | `web/src/components/ui/AgroMeshMark.vue` (current grid mark SVG, colours `#1A4A32 #2D7A4F #3DAA6A #E8A838 #0D2B1F`) | M | Rename to `IlevoraMark.vue`; imported by `FarmLogo.vue`, `FarmLogoCard.vue`, `Sidebar.vue` |
| Farm logo fallback and settings | `web/src/components/ui/FarmLogo.vue`, `web/src/components/settings/FarmLogoCard.vue` ("The app keeps showing the AgroMesh logo"), `web/src/composables/useFarmLogo.ts`, `useFarmLogo.test.ts` (7 hits) | L | Keep the rule: farm logo only on invoices and its preview |
| Mobile login | `mobile/lib/screens/auth/login_screen.dart` | L | |
| Mobile header | `mobile/lib/screens/main_shell.dart` (`AgroMark`, 'AgroMesh' text) | L | |
| Mobile marks | `mobile/lib/widgets/farm_mark.dart` (the "A" mark, `FarmMark`) | M | Replace drawing; widget name `AgroMark` used in several screens |
| Mobile app class/title | `mobile/lib/main.dart` (`AgroMeshApp`, `title: 'AgroMesh'`) | L | Class rename is internal |
| Mobile permission/error copy | `mobile/lib/services/file_picking.dart` (camera/file messages) | L | |
| iOS permission strings | `mobile/ios/Runner/Info.plist` (photo library, camera, location prompts) | L | Store review reads these; update wording and name |
| iOS display name | `mobile/ios/Runner/Info.plist` (`CFBundleDisplayName`, `CFBundleName`) | L | Set to "Ilevora" |
| Android launcher name | `mobile/android/app/src/main/AndroidManifest.xml` (`android:label="mobile"`) | L | The launcher label is currently literally "mobile", so it must be set regardless of the rename |
| Docs: top-level | `README.md` (title, brand colours "Forest/Palm/Harvest, Syne, Inter, JetBrains Mono", domain `agromesh.io`) | L | README type and colour info is already outdated; rewrite |
| Docs: web/mobile READMEs | `web/README.md`, `mobile/README.md` (title, "AgroMesh design tokens") | L | |
| Project instructions | `CLAUDE.md` (title line, 7 hits), `.claude/agents/spec-driven-builder.md` | L | Update intro, keep `agromesh_*` container names until infra is renamed |
| Feature docs | `docs/features/*` (about 160 hits across spec/plan files) | L | Historical; leave as is or add a note "formerly AgroMesh" |

## B. PDFs and API-facing text

| Where | File | Risk | Notes |
|---|---|---|---|
| Invoice PDFs | `api/AgroMesh.Infrastructure/Services/Invoices/*` | L | Search found no "AgroMesh" display strings in the PDF templates (only namespaces and one font error message). Confirm visually by rendering all 10 templates. Decide whether to add a "Generated with Ilevora" footer (brand decision) |
| Font error message | `InvoiceFonts.cs` line 28 (mentions assembly name) | L | Changes only if the assembly is renamed |
| Swagger title | `api/AgroMesh.API/Program.cs` ("AgroMesh API") | L | |
| Outbound User-Agent | `api/.../Weather/WeatherService.cs` line 141 (`AgroMesh/1.0 (+https://github.com/okeleyekabiru/agromesh)`) | M | Sent to Open-Meteo; change name but keep a working contact URL. Repo URL changes if the GitHub repo is renamed |
| Email/notifications | none found | | Re-check if email sending is added |

## C. Identity, auth and stored data (high care)

| Where | File | Risk | Notes |
|---|---|---|---|
| JWT issuer | `api/AgroMesh.API/appsettings.json` (`Jwt:Issuer: "AgroMesh"`), default in `JwtService.cs` line 19, env `Jwt__Issuer` in `render.yaml` | **H** | Changing the issuer invalidates every issued token (all users logged out). Change in one release, tell users, or accept both issuers for a transition |
| JWT signing key | `appsettings.json` (`agromesh-super-secret-key-...`) | **H (security, not branding)** | A real-looking secret is committed in config; rotate it in production regardless of the rename and keep it out of git |
| Mobile secure storage keys | `mobile/lib/services/auth_service.dart` (`agromesh_token`, `agromesh_user`, `agromesh_company_id`, `agromesh_farms`, `agromesh_active_farm_id`) | **H** | Renaming forces re-login. If renamed, read the old key, write the new, delete the old |
| Mobile SQLite file | `mobile/lib/services/database_service.dart` (`agromesh.db`) | **H** | Renaming orphans offline data not yet synced. Prefer keeping the file name, or migrate the file |
| Mobile package name | `mobile/pubspec.yaml` (`name: agromesh_mobile`) | M | Changing it rewrites every `package:agromesh_mobile/...` import in `lib/` and `test/` (about 50 test hits mention AgroMesh) |
| Mobile app id (Android) | `mobile/android/app/build.gradle` (`namespace`, `applicationId = com.example.mobile`), `kotlin/com/example/mobile/MainActivity.kt` (package dir) | **H** | New id = a different app to the stores: existing installs do not upgrade, users must reinstall. Play Store rejects `com.example.*`, so an id change is required before any Play release anyway. Choose the final id once (suggested `com.ilevora.app`, **VERIFY** domain). Moving the Kotlin package folder must match |
| Mobile app id (iOS) | `mobile/ios/Runner.xcodeproj/project.pbxproj` (6 lines, `com.example.mobile`, `com.example.mobile.RunnerTests`) | **H** | Same as Android; set before first App Store submission |
| App signing | release APK is debug-signed | **H (launch blocker)** | Create a real upload keystore before the first store release; a different key means a different app |

## D. Hosting, URLs, infrastructure

| Where | File | Risk | Notes |
|---|---|---|---|
| Hard-coded production API URL | `mobile/lib/services/api_client.dart` line 37 (`https://agromesh-api.onrender.com`) | **H** | Every shipped APK points here. Renaming the Render service changes this URL and breaks old APKs. Safer: add a custom domain (e.g. `api.ilevora.com`), ship the app pointing at it, and keep the onrender URL alive for the transition period |
| Render blueprint | `render.yaml` (services `agromesh-redis`, `agromesh-api`, issuer value `AgroMesh`) | **H** | Renaming services can create new ones and drop env/state; rename in the Render dashboard deliberately, not by editing the file blindly |
| Azure deploy script | `deploy/azure.ps1` (resource group `agromesh`, app `agromesh-api`, ghcr image `.../agromesh-api`) | M | Only matters if used; rename resources deliberately |
| Cron to API | `web/api/cron/daily-sync.ts` (comments reference `AgroMesh.API` paths) | L | Comment-only, but update if folders are renamed |
| Web hosting config | `web/vercel.json`, `web/nginx.conf`, `web/Dockerfile` | L | No "agromesh" text found; check any hostnames or CORS origins once the new domain exists |
| CORS and allowed origins | API config | M | Add new domain(s) before switching the web app over |
| Web package | `web/package.json` (`name: agromesh-web`) | L | Safe to rename; lockfile also contains the name |
| Domain | `agromesh.io` (README) | M | Redirect old domain to the new one; keep it renewed |
| Git repository and remote | repo folder `agromesh`, `github.com/okeleyekabiru/agromesh` (User-Agent) | M | Renaming the repo redirects on GitHub but breaks local paths, scripts and CI that use them |
| E2E test | `web/e2e/app.spec.ts` line 51 expects `.logo-text` to contain "AgroMesh" | L | Update the expected text (suite is largely red already per CLAUDE.md) |

## E. Local Docker stack (do not touch without a plan)

| Where | File | Risk | Notes |
|---|---|---|---|
| Containers | `api/docker-compose.yml`: `agromesh_db`, `agromesh_redis`, `agromesh_api`, `agromesh_web` | **H** | CLAUDE.md: do not stop or recreate `agromesh_*` containers unless asked. Rename only with the user's go-ahead |
| Database name/user/password | compose and `appsettings*.json` (`Database=agromesh;Username=agromesh;Password=agromesh_dev`) | **H** | Renaming the DB or user needs a dump/restore. These are internal; recommended to keep indefinitely |
| Volume | `agromesh_pgdata` | **H** | Renaming creates an empty volume. Never run `docker compose down -v` (CLAUDE.md). Keep the name |
| Dev settings | `api/AgroMesh.API/appsettings.Development.json`, `AgroMesh.API.http` | L | |

## F. C# code identity (optional, last, large)

About 95 percent of all hits. None of this is visible to users.

| Where | Scale | Risk | Notes |
|---|---|---|---|
| Solution and projects | `api/AgroMesh.slnx`, `AgroMesh.Domain`, `.Application`, `.Infrastructure`, `.API`, `.Tests` folders and `.csproj` files; `Dockerfile` copies each by name and runs `AgroMesh.API.dll` | M | Renaming folders and projects touches Dockerfile, compose volume mounts (`./AgroMesh.API/appsettings.Development.json`), and `dotnet test api/AgroMesh.slnx` in CLAUDE.md |
| Namespaces and usings | about 500 `.cs` files | M | Mechanical (IDE rename) but huge diff; conflicts with other agents editing concurrently |
| `AgroMeshDbContext` and migrations | `AgroMeshDbContext.cs`, `AgroMeshDbContextModelSnapshot.cs`, all `*.Designer.cs` (the model snapshot has 63 hits) | **H** | Migrations reference the context type and entity namespaces by name; renaming can desync the snapshot and cause EF to generate a bogus migration against production. Needs a careful, tested change |
| Entity type names in snapshot | `AgroMesh.Domain.Entities.*` strings | **H** | Same as above |
| Color/theme class names | `AgroColors`, `AgroTheme` in `mobile/lib/theme/app_theme.dart` (comments say "AGROMESH") | L | Rename to `IlevoraColors` optional; many files import them |
| Web CSS tokens | `--forest`, `--palm`, `--harvest` in `web/src/assets/main.css` | L | Token names are brand-neutral; update values (see brand guide palette) rather than names |

**Recommendation:** do A, B, D (with custom domain first) and the store identity in C. Leave E and F as AgroMesh internally until there is a clear reason; they cost the most and risk the most, and users never see them.

## Suggested sequence

1. Reserve domain, handles, product trademark (see brand guide section 2).
2. Add custom API domain; ship config accepting it; keep onrender URL alive.
3. Decide final Android/iOS application id and real signing keys.
4. Replace logo/mark, app names, titles, landing and permission strings (A, B).
5. Release; monitor logins (do not change JWT issuer or storage keys in the same release).
6. Optional later: JWT issuer, storage-key migration, repo rename, internal renames.
