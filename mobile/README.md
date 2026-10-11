# AgroMesh Mobile — Flutter

Offline-first activity logging app for field supervisors.
Runs on Android and iOS from a single codebase.

## Prerequisites

- Flutter SDK 3.10+ (flutter.dev/docs/get-started/install)
- Android Studio or VS Code with Flutter extension
- Android emulator or physical device (Android 6+)

## Setup

```bash
# 1. Install dependencies
flutter pub get

# 2. Run on connected device / emulator
flutter run

# 3. Build release APK (Android)
flutter build apk --release

# 4. Build release IPA (iOS — requires macOS + Xcode)
flutter build ios --release
```

## Project Structure

```
lib/
├── main.dart                    # App entry point
├── theme/
│   └── app_theme.dart           # AgroMesh design tokens (colors, fonts)
├── models/
│   └── models.dart              # Domain models + enums
├── stores/
│   └── app_store.dart           # State management (Provider + ChangeNotifier)
├── services/
│   ├── database_service.dart    # SQLite — offline-first storage
│   └── sync_service.dart        # Online sync to backend API
├── widgets/
│   └── widgets.dart             # Reusable UI components
└── screens/
    ├── main_shell.dart          # Bottom nav + app shell
    ├── dashboard/
    │   └── dashboard_screen.dart
    ├── activity/
    │   └── log_activity_screen.dart  # 3-step log flow
    ├── blocks_screen.dart
    └── workers_screen.dart
```

## Offline-First Architecture

```
Field Supervisor taps Submit
         ↓
   SQLite (local)          ← Always writes here first
         ↓
   Background sync         ← Triggers when internet detected
         ↓
   Backend API             ← ASP.NET Core on Azure
         ↓
   PostgreSQL              ← Central database
```

## Key Features

- Log activities offline — no internet required
- Automatic sync when connectivity returns
- SQLite persists all data locally
- Sync status visible on every activity (synced/pending/failed)
- Pull-to-refresh triggers manual sync
- Supports Android 6+ and iOS 12+

## Design System

Matches AgroMesh web exactly:
- Colors: Forest #0D2B1F, Palm #3DAA6A, Harvest #E8A838
- Fonts: Syne (display), Inter (body), JetBrains Mono (data)
- All tokens in `lib/theme/app_theme.dart`

## Next Steps

- [ ] Connect sync_service.dart to real backend URL
- [ ] Add GPS location capture on activity log
- [ ] Add camera/photo capture for field observations
- [ ] Push notifications for sync completion
- [ ] Biometric authentication
