# AgroMesh Web — Farm Operating System

Vue 3 + Vite + TypeScript + Pinia + Vue Router

## Prerequisites

- Node.js 18+ (download from nodejs.org)
- npm or yarn

## Setup

```bash
# 1. Install dependencies
npm install

# 2. Start dev server (opens at http://localhost:3000)
npm run dev

# 3. Build for production
npm run build
```

## Project Structure

```
src/
├── assets/
│   └── main.css          # Global styles + AgroMesh design tokens
├── components/
│   ├── ui/               # Reusable UI components
│   │   ├── KpiCard.vue
│   │   ├── StatBadge.vue
│   │   ├── AlertBanner.vue
│   │   ├── EmptyState.vue
│   │   ├── FilterPill.vue
│   │   ├── BlockRow.vue
│   │   ├── ActivityRow.vue
│   │   ├── TransactionRow.vue
│   │   └── WorkerRow.vue
│   ├── layout/           # App shell components
│   │   ├── AppShell.vue
│   │   ├── Sidebar.vue
│   │   └── Topbar.vue
│   └── modal/
│       └── LogActivityModal.vue
├── data/
│   └── mock.ts           # Seed data for development
├── router/
│   └── index.ts          # Vue Router config
├── stores/
│   └── agro.ts           # Pinia store (state + actions)
├── types/
│   └── index.ts          # TypeScript domain types
├── views/                # Page components
│   ├── DashboardView.vue
│   ├── ActivitiesView.vue
│   ├── BlocksView.vue
│   ├── FinanceView.vue
│   ├── WorkersView.vue
│   └── ReportsView.vue
├── App.vue
└── main.ts
```

## Brand System

| Token   | Hex       | Usage                          |
|---------|-----------|--------------------------------|
| Forest  | #0D2B1F   | Primary dark, sidebar          |
| Canopy  | #1A4A32   | Surface dark                   |
| Leaf    | #2D7A4F   | Primary action, buttons        |
| Palm    | #3DAA6A   | Accent, highlights             |
| Harvest | #E8A838   | Alerts, attention              |
| Mist    | #F0F4F1   | Page background                |
| Fog     | #E2EBE5   | Borders, dividers              |
| Stone   | #6B7C72   | Secondary text                 |

Fonts: Syne 800 (display) · Inter (body) · JetBrains Mono (data/labels)

## Tech Stack

- **Framework**: Vue 3 (Composition API)
- **Build**: Vite
- **Language**: TypeScript
- **State**: Pinia
- **Router**: Vue Router 4
- **Styles**: TailwindCSS + CSS custom properties
- **Mobile**: Flutter (separate project — /mobile)

## Next Steps

- [ ] Connect Mono API for real bank transactions
- [ ] Connect Anthropic API for transaction categorization
- [ ] Build Flutter mobile app for offline activity logging
- [ ] Set up ASP.NET Core backend
- [ ] Configure PostgreSQL + PostGIS
- [ ] Deploy to Azure
