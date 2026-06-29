# AgroMesh — Farm Operating System

> Extract this folder to `C:\Users\kabio\source\repos\agromesh`

---

## Project Structure

```
agromesh/
├── web/          # Vue 3 + Vite — Manager dashboard (browser)
└── mobile/       # Flutter — Field supervisor app (Android + iOS)
```

---

## Quick Start

### Web Dashboard

```bash
cd web
npm install
npm run dev
# Opens at http://localhost:3000
```

### Mobile App

```bash
cd mobile
flutter pub get
flutter run
```

---

## What Each App Does

| App    | User              | Purpose                                         |
|--------|-------------------|-------------------------------------------------|
| Web    | Farm owner        | View P&L, sections, workers, finance dashboard  |
| Mobile | Field supervisor  | Log daily activities offline, sync when online  |

---

## Tech Stack

| Layer     | Technology                          |
|-----------|-------------------------------------|
| Web       | Vue 3, Vite, TypeScript, Pinia      |
| Mobile    | Flutter, Dart, SQLite (sqflite)     |
| Backend   | ASP.NET Core, C# (to be built)      |
| Database  | PostgreSQL + PostGIS (Azure)        |
| AI        | Anthropic Claude API (categorization)|
| Banking   | Mono API (transaction sync)         |

---

## Brand

Forest `#0D2B1F` · Palm `#3DAA6A` · Harvest `#E8A838`  
Syne 800 · Inter · JetBrains Mono

Domain: agromesh.io
# agromesh
