# AgroMesh

Farm management: .NET 8 API (`api/`), Vue 3 + Vite + Tailwind web (`web/`), Flutter mobile (`mobile/`). Typeface everywhere: Plus Jakarta Sans (web via fontsource, mobile bundled as `PlusJakartaSans`, API embeds static weights for PDFs).

## Run, test, build

| Area | Command (from the folder) |
|---|---|
| API tests | `dotnet test api/AgroMesh.slnx` (from repo root) |
| Web | `npx vue-tsc --noEmit`, `npx vitest run`, `npx vite build` (in `web/`). The old Playwright suite in `web/e2e` is largely red for unrelated reasons, so do not run it wholesale. |
| Mobile | `C:\Users\kabio\flutter-stable\bin\flutter analyze` and `... test` (in `mobile/`). Use this SDK (3.47.9); the system Flutter 3.26 cannot build the project. The 2 `Path` hide warnings in `map_pick_screen.dart` and `satellite_screen.dart` are known. |
| APK | `flutter build apk --release` in `mobile/` (about 12 minutes). Debug-signed, app id `com.example.mobile`. Release builds call `https://agromesh-api.onrender.com`; debug uses `10.0.2.2:5000` (emulator) or `API_BASE_URL`. |

## Local stack (Docker, from `api/`)

`api/docker-compose.yml`: web `http://localhost:5173`, API `http://localhost:5000`, Postgres `5433`, Redis `6380`. Containers: `agromesh_web`, `agromesh_api`, `agromesh_db`, `agromesh_redis`.

- Rebuild after code changes (images bake the code in): `docker compose build web api` then `docker compose up -d --no-deps web api`. Add `--no-cache` if a build looks stale.
- The API applies EF migrations on startup. Back up first: `docker exec agromesh_db pg_dump -U agromesh -Fc agromesh -f /tmp/backup.dump`, then `docker cp`.
- Never run `docker compose down -v` (it deletes the database volume). Do not stop or recreate the `agromesh_*` containers unless asked. Throwaway test databases get their own unique container names and random ports.

## Product rules

- The farm logo is used ONLY on invoices (PDF) and previewed in settings. It must never replace the AgroMesh logo in the app chrome.
- Invoices: 10 templates (`ngf` default, plus band, sidebar, minimal, callout, cards, ledger, angle, anchor, duo). Brand colours resolve invoice, then farm, then template default (`#138A44` green and `#F5C90C` yellow). Strict `#RRGGBB`. Contracts: `docs/features/farm-logo-invoices-vendor-bills/api-contract.md` and `api-contract-v2.md`; spec in `spec.md`.
- Vendor invoices attach only to Received orders and never create a finance transaction.

## Working with agents (token budget matters)

- Several agents and the user edit this repo at once. Run `git status` first. Never revert, stash, reset or overwrite files you did not change, and never `git stash drop` or `git clean`. Do not commit or push unless asked; commit only the files you own.
- The 5-hour usage limit is shared by all agents. Run at most two agents at once, never two on the same files.
- Continue an existing agent with SendMessage for follow-ups in its area instead of spawning a new one. Start a fresh agent only when none exists for that area, the user asks, or a milestone is finished and its history is huge (then give it a short handoff: decisions, file paths, contract files).
- Keep briefs short (point to contract/spec files), ask for reports of about 200 words, and prefer the builder's own tests plus one focused independent check over repeated full QA rounds. Tiny edits are done inline.
