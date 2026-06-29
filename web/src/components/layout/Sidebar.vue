<template>
  <aside class="sidebar">
    <!-- Logo -->
    <div class="sidebar-logo">
      <svg width="28" height="28" viewBox="0 0 72 72" fill="none">
        <rect width="72" height="72" rx="16" fill="#1A4A32"/>
        <line x1="24" y1="14" x2="24" y2="58" stroke="#2D7A4F" stroke-width="1.5" stroke-dasharray="2 3"/>
        <line x1="48" y1="14" x2="48" y2="58" stroke="#2D7A4F" stroke-width="1.5" stroke-dasharray="2 3"/>
        <line x1="14" y1="24" x2="58" y2="24" stroke="#2D7A4F" stroke-width="1.5" stroke-dasharray="2 3"/>
        <line x1="14" y1="48" x2="58" y2="48" stroke="#2D7A4F" stroke-width="1.5" stroke-dasharray="2 3"/>
        <circle cx="24" cy="24" r="3.5" fill="#3DAA6A"/>
        <circle cx="48" cy="24" r="3.5" fill="#3DAA6A"/>
        <circle cx="24" cy="48" r="3.5" fill="#3DAA6A"/>
        <circle cx="48" cy="48" r="3.5" fill="#3DAA6A"/>
        <circle cx="36" cy="36" r="7" fill="#E8A838"/>
        <circle cx="36" cy="36" r="3.5" fill="#0D2B1F"/>
        <line x1="24" y1="24" x2="36" y2="36" stroke="#3DAA6A" stroke-width="1.5"/>
        <line x1="48" y1="24" x2="36" y2="36" stroke="#3DAA6A" stroke-width="1.5"/>
        <line x1="24" y1="48" x2="36" y2="36" stroke="#3DAA6A" stroke-width="1.5"/>
        <line x1="48" y1="48" x2="36" y2="36" stroke="#3DAA6A" stroke-width="1.5"/>
      </svg>
      <span class="logo-text">Agro<span>Mesh</span></span>
    </div>

    <!-- Farm Context -->
    <div class="sidebar-farm">
      <div class="label-mono" style="margin-bottom:4px;">Active Farm</div>
      <div class="farm-name">{{ store.farm.name }}</div>
      <div class="farm-sub">{{ store.farm.location }} · {{ store.farm.totalHectares }} acres</div>
    </div>

    <!-- Nav -->
    <nav class="sidebar-nav">
      <div class="label-mono" style="padding:12px 16px 4px;opacity:0.6;">Operations</div>
      <RouterLink
        v-for="item in navItems"
        :key="item.id"
        :to="item.path"
        class="nav-item"
        active-class="active"
      >
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor"
             stroke-width="2" stroke-linecap="round" stroke-linejoin="round"
             v-html="item.icon" />
        {{ item.label }}
      </RouterLink>
    </nav>

    <!-- Sync Status -->
    <div class="sidebar-bottom">
      <div class="sync-status">
        <div class="sync-dot" />
        <span class="sync-text">SYNCED · {{ store.lastSynced }}</span>
      </div>
    </div>
  </aside>
</template>

<script setup lang="ts">
import { useAgroStore } from '@/stores/agro'

const store = useAgroStore()

const navItems = [
  { id: 'dashboard',  path: '/',           label: 'Dashboard',  icon: '<rect x="3" y="3" width="7" height="7"/><rect x="14" y="3" width="7" height="7"/><rect x="3" y="14" width="7" height="7"/><rect x="14" y="14" width="7" height="7"/>' },
  { id: 'activities', path: '/activities', label: 'Activities', icon: '<path d="M9 11l3 3L22 4"/><path d="M21 12v7a2 2 0 01-2 2H5a2 2 0 01-2-2V5a2 2 0 012-2h11"/>' },
  { id: 'sections',   path: '/sections',   label: 'Sections',   icon: '<path d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/>' },
  { id: 'finance',    path: '/finance',    label: 'Finance',    icon: '<line x1="12" y1="1" x2="12" y2="23"/><path d="M17 5H9.5a3.5 3.5 0 000 7h5a3.5 3.5 0 010 7H6"/>' },
  { id: 'workers',    path: '/workers',    label: 'Workers',    icon: '<path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 00-3-3.87"/><path d="M16 3.13a4 4 0 010 7.75"/>' },
  { id: 'reports',    path: '/reports',    label: 'Reports',    icon: '<line x1="18" y1="20" x2="18" y2="10"/><line x1="12" y1="20" x2="12" y2="4"/><line x1="6" y1="20" x2="6" y2="14"/>' },
]
</script>

<style scoped>
.sidebar {
  width: 220px;
  min-width: 220px;
  background: var(--forest);
  display: flex;
  flex-direction: column;
  border-right: 1px solid var(--canopy);
}
.sidebar-logo {
  padding: 20px 20px 16px;
  border-bottom: 1px solid var(--canopy);
  display: flex;
  align-items: center;
  gap: 10px;
}
.logo-text {
  font-family: 'Syne', sans-serif;
  font-weight: 800;
  font-size: 20px;
  color: white;
  letter-spacing: -0.02em;
  line-height: 1;
}
.logo-text span { color: var(--palm); }
.sidebar-farm {
  padding: 12px 16px;
  border-bottom: 1px solid var(--canopy);
}
.farm-name { font-size: 12px; font-weight: 600; color: white; }
.farm-sub { font-size: 10px; color: var(--stone); margin-top: 1px; }
.sidebar-nav { flex: 1; padding: 8px 0; overflow-y: auto; }
.nav-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 9px 16px;
  font-size: 12px;
  font-weight: 500;
  color: var(--stone);
  text-decoration: none;
  transition: all 0.15s;
  border-right: 2px solid transparent;
}
.nav-item:hover { color: white; background: rgba(255,255,255,0.04); }
.nav-item.active { color: white; background: rgba(61,170,106,0.1); border-right-color: var(--palm); }
.nav-item svg { opacity: 0.5; flex-shrink: 0; }
.nav-item.active svg { opacity: 1; }
.sidebar-bottom { padding: 12px 16px; border-top: 1px solid var(--canopy); }
.sync-status {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 10px;
  background: rgba(0,200,150,0.08);
  border-radius: 8px;
  border: 1px solid rgba(0,200,150,0.15);
}
.sync-text {
  font-family: 'JetBrains Mono', monospace;
  font-size: 9px;
  color: var(--data);
  letter-spacing: 0.08em;
}
</style>
