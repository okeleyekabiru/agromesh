<template>
  <div class="topbar">
    <button class="menu-btn" aria-label="Open navigation menu" @click="drawer.toggle()">
      <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><line x1="4" y1="7" x2="20" y2="7"/><line x1="4" y1="12" x2="20" y2="12"/><line x1="4" y1="17" x2="20" y2="17"/></svg>
    </button>
    <div class="topbar-left">
      <div class="page-title">{{ route.meta.title as string }}</div>
      <div class="page-sub">{{ route.meta.sub as string }}</div>
    </div>
    <div class="topbar-right">
      <button v-if="route.name === 'finance'" class="btn btn-ghost btn-sm" aria-label="Export CSV">
        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round">
          <path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4M7 10l5 5 5-5M12 15V3"/>
        </svg>
        <span class="btn-label">Export CSV</span>
      </button>
      <button
        v-if="route.name === 'blocks'"
        class="btn btn-primary log-btn"
        @click="showAddBlockModal = true"
        aria-label="Add a new block"
      >
        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round">
          <line x1="12" y1="5" x2="12" y2="19"/>
          <line x1="5" y1="12" x2="19" y2="12"/>
        </svg>
        <span class="btn-label">Add Block</span>
      </button>
      <button v-else class="btn btn-primary log-btn" @click="showModal = true" aria-label="Log a new activity">
        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round">
          <line x1="12" y1="5" x2="12" y2="19"/>
          <line x1="5" y1="12" x2="19" y2="12"/>
        </svg>
        <span class="btn-label">Log Activity</span>
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { inject, type Ref } from 'vue'
import { useRoute } from 'vue-router'
import { useNavDrawer } from '@/composables/useNavDrawer'

const route = useRoute()
const drawer = useNavDrawer()
const showModal = inject<Ref<boolean>>('showModal')!
const showAddBlockModal = inject<Ref<boolean>>('showAddBlockModal')!
</script>

<style scoped>
.topbar {
  background: white;
  border-bottom: 1px solid var(--fog);
  padding: 0 24px;
  height: 54px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-shrink: 0;
  box-shadow: 0 1px 0 var(--fog);
}
.topbar-left { display: flex; flex-direction: column; gap: 1px; min-width: 0; flex: 1; }
.menu-btn {
  display: none; align-items: center; justify-content: center; flex-shrink: 0;
  width: 40px; height: 40px; margin-left: -8px; margin-right: 6px;
  border: 0; background: transparent; color: var(--forest); border-radius: 10px; cursor: pointer;
}
.menu-btn:hover { background: var(--fog); }
.page-title, .page-sub { white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.page-title {
  font-family: var(--font-sans);
  font-weight: 700;
  font-size: 16px;
  color: var(--forest);
  letter-spacing: -0.01em;
  line-height: 1.1;
}
.page-sub {
  font-family: var(--font-sans);
  font-size: 11px;
  color: var(--stone);
  letter-spacing: 0.12em;
  text-transform: uppercase;
}
.topbar-right { display: flex; align-items: center; gap: 10px; }

.log-btn {
  position: relative;
  overflow: hidden;
}
.log-btn::after {
  content: '';
  position: absolute;
  inset: 0;
  background: rgba(255,255,255,0.15);
  opacity: 0;
  transition: opacity var(--dur-fast);
  border-radius: inherit;
}
.log-btn:hover::after { opacity: 1; }
@media (max-width: 900px) {
  .topbar { padding: 0 14px; }
  .menu-btn { display: inline-flex; }
  .topbar-right { flex-shrink: 0; gap: 8px; }
}
@media (max-width: 560px) {
  .page-sub { display: none; }
  .btn-label { display: none; }
  .topbar-right .btn { min-width: 40px; min-height: 40px; padding: 0 12px; justify-content: center; }
}
</style>
