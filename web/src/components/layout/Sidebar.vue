<template>
  <aside class="sidebar">
    <!-- Logo -->
    <div class="sidebar-logo">
      <div class="logo-icon">
        <svg width="26" height="26" viewBox="0 0 72 72" fill="none">
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
      </div>
      <span class="logo-text">Agro<span class="logo-accent">Mesh</span></span>
    </div>

    <!-- Farm Context / Switcher -->
    <div class="sidebar-farm">
      <button
        class="farm-switcher-btn"
        @click="farmMenuOpen = !farmMenuOpen"
        :aria-expanded="farmMenuOpen"
        aria-label="Switch farm"
      >
        <div class="farm-switcher-text">
          <div class="farm-label">ACTIVE FARM</div>
          <div class="farm-name">{{ store.farm?.name ?? store.activeFarmSummary?.name ?? '…' }}</div>
          <div class="farm-meta">
            <span>{{ store.farm?.location ?? 'Loading…' }}</span>
            <template v-if="store.farm?.area?.displayValue">
              <span class="farm-sep">·</span>
              <span class="farm-area">{{ store.farm.area.displayValue }} {{ store.farm.area.displayUnit }}</span>
            </template>
          </div>
        </div>
        <svg class="farm-chevron" :class="{ 'farm-chevron--open': farmMenuOpen }" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="6 9 12 15 18 9"/></svg>
      </button>

      <div v-if="farmMenuOpen" class="farm-menu-backdrop" @click="farmMenuOpen = false" />
      <div v-if="farmMenuOpen" class="farm-menu">
        <button
          v-for="f in store.farms" :key="f.id"
          class="farm-menu-item"
          :class="{ 'farm-menu-item--active': f.id === store.activeFarmId }"
          @click="selectFarm(f.id)"
        >
          <span class="farm-menu-name">{{ f.name }}</span>
          <span v-if="f.setupStatus === 0" class="farm-menu-draft">DRAFT</span>
          <svg v-if="f.id === store.activeFarmId" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="20 6 9 17 4 12"/></svg>
        </button>
        <button class="farm-menu-add" @click="openAddFarm">
          <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/></svg>
          Add Farm
        </button>
      </div>
    </div>

    <!-- Nav -->
    <nav class="sidebar-nav" aria-label="Main navigation">
      <div class="nav-section-label">Operations</div>
      <RouterLink
        v-for="item in navItems"
        :key="item.id"
        :to="item.path"
        class="nav-item"
        active-class="active"
        :style="`--ni: ${item._i}`"
        :aria-label="item.label"
      >
        <span class="nav-icon">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor"
               stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"
               v-html="item.icon" />
        </span>
        <span class="nav-label">{{ item.label }}</span>
        <span v-if="item.badge" class="nav-badge">{{ item.badge }}</span>
        <span class="nav-indicator" />
      </RouterLink>
    </nav>

    <!-- Bottom status + logout -->
    <div class="sidebar-bottom">
      <div v-if="store.lastSynced" class="sync-card">
        <span class="sync-dot" />
        <div class="sync-info">
          <div class="sync-label">LIVE</div>
          <div class="sync-time">{{ store.lastSynced }}</div>
        </div>
      </div>
      <div v-else class="sync-card sync-idle">
        <span class="sync-dot-idle" />
        <div class="sync-info">
          <div class="sync-label" style="color:var(--stone);">OFFLINE</div>
        </div>
      </div>
      <div v-if="store.currentUser" class="user-row">
        <div class="user-info">
          <div class="user-name">{{ store.currentUser.fullName }}</div>
          <div class="user-email">{{ store.currentUser.email }}</div>
        </div>
        <button class="logout-btn" @click="handleLogout" aria-label="Sign out">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor"
               stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round">
            <path d="M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4"/>
            <polyline points="16 17 21 12 16 7"/>
            <line x1="21" y1="12" x2="9" y2="12"/>
          </svg>
        </button>
      </div>
    </div>
  </aside>

  <AddFarmModal v-if="showAddFarmModal" @close="showAddFarmModal = false" />
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAgroStore } from '@/stores/agro'
import { useFeatureFlag } from '@/composables/useFeatureFlag'
import AddFarmModal from '@/components/modal/AddFarmModal.vue'

const store = useAgroStore()
const router = useRouter()
const { hasFeature } = useFeatureFlag()

function handleLogout() {
  store.logout()
  router.push({ name: 'landing' })
}

// ── Farm switcher ──
const farmMenuOpen = ref(false)
const showAddFarmModal = ref(false)

async function selectFarm(farmId: string) {
  farmMenuOpen.value = false
  if (farmId === store.activeFarmId) return
  await store.switchFarm(farmId)
  router.push({ name: 'dashboard' })
}

function openAddFarm() {
  farmMenuOpen.value = false
  showAddFarmModal.value = true
}

onMounted(() => {
  if (!store.farms.length) store.fetchFarms()
})

const ALL_NAV = [
  {
    id: 'dashboard',
    path: '/dashboard',
    label: 'Dashboard',
    icon: '<rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/>',
    feature: null as string | null,
    badgeFn: () => null as number | null
  },
  {
    id: 'activities',
    path: '/activities',
    label: 'Activities',
    icon: '<path d="M9 11l3 3L22 4"/><path d="M21 12v7a2 2 0 01-2 2H5a2 2 0 01-2-2V5a2 2 0 012-2h11"/>',
    feature: null,
    badgeFn: () => null
  },
  {
    id: 'sections',
    path: '/sections',
    label: 'Sections',
    icon: '<path d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/>',
    feature: null,
    badgeFn: () => null
  },
  {
    id: 'finance',
    path: '/finance',
    label: 'Finance',
    icon: '<line x1="12" y1="1" x2="12" y2="23"/><path d="M17 5H9.5a3.5 3.5 0 000 7h5a3.5 3.5 0 010 7H6"/>',
    feature: 'finance',
    badgeFn: () => store.pendingReviews > 0 ? store.pendingReviews : null
  },
  {
    id: 'workers',
    path: '/workers',
    label: 'Workers',
    icon: '<path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 00-3-3.87"/><path d="M16 3.13a4 4 0 010 7.75"/>',
    feature: 'workers',
    badgeFn: () => null
  },
  {
    id: 'inventory',
    path: '/inventory',
    label: 'Inventory',
    icon: '<path d="M21 16V8a2 2 0 00-1-1.73l-7-4a2 2 0 00-2 0l-7 4A2 2 0 003 8v8a2 2 0 001 1.73l7 4a2 2 0 002 0l7-4A2 2 0 0021 16z"/><polyline points="3.27 6.96 12 12.01 20.73 6.96"/><line x1="12" y1="22.08" x2="12" y2="12"/>',
    feature: 'inventory',
    badgeFn: () => store.lowStockCount > 0 ? store.lowStockCount : null
  },
  {
    id: 'reports',
    path: '/reports',
    label: 'Reports',
    icon: '<line x1="18" y1="20" x2="18" y2="10"/><line x1="12" y1="20" x2="12" y2="4"/><line x1="6" y1="20" x2="6" y2="14"/>',
    feature: 'reports',
    badgeFn: () => null
  },
]

const navItems = computed(() =>
  ALL_NAV
    .filter(item => !item.feature || hasFeature(item.feature))
    .map((item, i) => ({ ...item, badge: item.badgeFn(), _i: i }))
)
</script>

<style scoped>
.sidebar {
  width: 224px;
  min-width: 224px;
  background: var(--forest);
  display: flex;
  flex-direction: column;
  border-right: 1px solid rgba(255,255,255,0.05);
  overflow: hidden;
}

/* ── Logo ── */
.sidebar-logo {
  padding: 18px 18px 14px;
  border-bottom: 1px solid rgba(255,255,255,0.06);
  display: flex;
  align-items: center;
  gap: 10px;
  flex-shrink: 0;
}
.logo-icon { flex-shrink: 0; }
.logo-text {
  font-family: 'Syne', sans-serif;
  font-weight: 800;
  font-size: 19px;
  color: white;
  letter-spacing: -0.03em;
  line-height: 1;
}
.logo-accent { color: var(--palm); }

/* ── Farm Context / Switcher ── */
.sidebar-farm {
  padding: 12px 16px;
  border-bottom: 1px solid rgba(255,255,255,0.06);
  flex-shrink: 0;
  position: relative;
}
.farm-switcher-btn {
  display: flex;
  align-items: flex-start;
  gap: 6px;
  width: 100%;
  background: none;
  border: none;
  padding: 0;
  cursor: pointer;
  text-align: left;
}
.farm-switcher-text { flex: 1; min-width: 0; }
.farm-chevron {
  flex-shrink: 0;
  margin-top: 3px;
  color: rgba(255,255,255,0.35);
  transition: transform var(--dur-fast);
}
.farm-chevron--open { transform: rotate(180deg); }

.farm-menu-backdrop {
  position: fixed;
  inset: 0;
  z-index: 40;
}
.farm-menu {
  position: absolute;
  top: 100%;
  left: 12px;
  right: 12px;
  z-index: 50;
  margin-top: 4px;
  background: var(--canopy);
  border: 1px solid rgba(255,255,255,0.1);
  border-radius: 10px;
  box-shadow: 0 12px 32px rgba(0,0,0,0.4);
  padding: 6px;
  animation: scale-in 150ms var(--ease-out) both;
}
.farm-menu-item {
  display: flex;
  align-items: center;
  gap: 6px;
  width: 100%;
  padding: 8px 10px;
  border-radius: 7px;
  background: none;
  border: none;
  cursor: pointer;
  color: rgba(255,255,255,0.75);
  font-size: 11.5px;
  text-align: left;
}
.farm-menu-item:hover { background: rgba(255,255,255,0.06); }
.farm-menu-item--active { color: #fff; font-weight: 600; }
.farm-menu-name { flex: 1; min-width: 0; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.farm-menu-draft {
  font-family: 'JetBrains Mono', monospace;
  font-size: 8px;
  letter-spacing: 0.06em;
  padding: 1px 6px;
  border-radius: 4px;
  background: rgba(232,168,56,0.15);
  color: var(--harvest);
  flex-shrink: 0;
}
.farm-menu-add {
  display: flex;
  align-items: center;
  gap: 6px;
  width: 100%;
  padding: 8px 10px;
  margin-top: 2px;
  border-radius: 7px;
  background: none;
  border: 1px dashed rgba(255,255,255,0.15);
  cursor: pointer;
  color: var(--palm);
  font-size: 11.5px;
  text-align: left;
}
.farm-menu-add:hover { background: rgba(61,170,106,0.1); }

.farm-label {
  font-family: 'JetBrains Mono', monospace;
  font-size: 8px;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: rgba(107,124,114,0.7);
  margin-bottom: 4px;
}
.farm-name {
  font-size: 12px;
  font-weight: 600;
  color: white;
  line-height: 1.3;
}
.farm-meta {
  display: flex;
  align-items: center;
  gap: 5px;
  font-size: 10px;
  color: var(--stone);
  margin-top: 2px;
}
.farm-sep { opacity: 0.4; }
.farm-area {
  font-family: 'JetBrains Mono', monospace;
  font-size: 9px;
}

/* ── Nav ── */
.sidebar-nav { flex: 1; padding: 8px 0 8px; overflow-y: auto; overflow-x: hidden; }

.nav-section-label {
  font-family: 'JetBrains Mono', monospace;
  font-size: 8px;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: rgba(107,124,114,0.55);
  padding: 8px 16px 6px;
}

.nav-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 16px;
  min-height: 44px;
  font-size: 12px;
  font-weight: 500;
  color: var(--stone);
  text-decoration: none;
  position: relative;
  cursor: pointer;
  transition:
    color       var(--dur-base) var(--ease-io),
    background  var(--dur-base) var(--ease-io);
  /* stagger on page load */
  animation: fade-up var(--dur-slow) var(--ease-out) both;
  animation-delay: calc(var(--ni, 0) * 40ms + 80ms);
}

.nav-item::before {
  content: '';
  position: absolute;
  inset: 3px 8px;
  border-radius: 8px;
  background: transparent;
  transition: background var(--dur-base) var(--ease-io);
  z-index: 0;
}

.nav-item:hover::before { background: rgba(255,255,255,0.05); }
.nav-item.active::before { background: rgba(61,170,106,0.12); }

.nav-item > * { position: relative; z-index: 1; }

.nav-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 20px;
  height: 20px;
  flex-shrink: 0;
  opacity: 0.45;
  transition: opacity var(--dur-base);
}
.nav-item:hover .nav-icon { opacity: 0.7; }
.nav-item.active .nav-icon { opacity: 1; }

.nav-label { flex: 1; letter-spacing: 0.01em; }

.nav-indicator {
  position: absolute;
  left: 0;
  top: 50%;
  transform: translateY(-50%) scaleY(0);
  width: 3px;
  height: 28px;
  background: var(--palm);
  border-radius: 0 3px 3px 0;
  transition: transform var(--dur-base) var(--ease-spring);
}
.nav-item.active .nav-indicator { transform: translateY(-50%) scaleY(1); }
.nav-item.active { color: white; }

.nav-badge {
  min-width: 18px;
  height: 18px;
  border-radius: 9px;
  background: var(--harvest);
  color: var(--forest);
  font-family: 'JetBrains Mono', monospace;
  font-size: 9px;
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0 5px;
  flex-shrink: 0;
  animation: scale-in 200ms var(--ease-spring) both;
}

/* ── Bottom Status ── */
.sidebar-bottom {
  padding: 12px;
  border-top: 1px solid rgba(255,255,255,0.06);
  flex-shrink: 0;
}
.sync-card {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 10px;
  background: rgba(0,200,150,0.07);
  border-radius: 8px;
  border: 1px solid rgba(0,200,150,0.14);
}
.sync-card.sync-idle {
  background: rgba(255,255,255,0.04);
  border-color: rgba(255,255,255,0.08);
}
.sync-info { display: flex; flex-direction: column; gap: 1px; }
.sync-label {
  font-family: 'JetBrains Mono', monospace;
  font-size: 8px;
  letter-spacing: 0.14em;
  color: var(--data);
}
.sync-time {
  font-family: 'JetBrains Mono', monospace;
  font-size: 10px;
  color: rgba(255,255,255,0.5);
}
.sync-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: var(--data);
  flex-shrink: 0;
  box-shadow: 0 0 0 2px rgba(0,200,150,0.25);
}
.sync-dot-idle {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: var(--stone);
  flex-shrink: 0;
}

/* User row + logout */
.user-row {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-top: 8px;
  padding: 8px 10px;
  background: rgba(255,255,255,0.04);
  border-radius: 8px;
  border: 1px solid rgba(255,255,255,0.06);
}
.user-info { flex: 1; min-width: 0; }
.user-name {
  font-size: 11px;
  font-weight: 600;
  color: rgba(255,255,255,0.85);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.user-email {
  font-family: 'JetBrains Mono', monospace;
  font-size: 9px;
  color: var(--stone);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.logout-btn {
  width: 28px;
  height: 28px;
  border-radius: 6px;
  border: 1px solid rgba(255,255,255,0.08);
  background: transparent;
  color: var(--stone);
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  flex-shrink: 0;
  transition: background var(--dur-fast), color var(--dur-fast);
}
.logout-btn:hover {
  background: rgba(220,60,60,0.15);
  color: #F87171;
  border-color: rgba(220,60,60,0.25);
}
</style>
