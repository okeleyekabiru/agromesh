<template>
  <div class="topbar">
    <div class="topbar-left">
      <div class="page-title">{{ route.meta.title as string }}</div>
      <div class="page-sub">{{ route.meta.sub as string }}</div>
    </div>
    <div class="topbar-right">
      <button v-if="route.name === 'finance'" class="btn btn-ghost btn-sm" aria-label="Export CSV">
        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round">
          <path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4M7 10l5 5 5-5M12 15V3"/>
        </svg>
        Export CSV
      </button>
      <button
        v-if="route.name === 'sections'"
        class="btn btn-primary log-btn"
        @click="showAddSectionModal = true"
        aria-label="Add a new section"
      >
        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round">
          <line x1="12" y1="5" x2="12" y2="19"/>
          <line x1="5" y1="12" x2="19" y2="12"/>
        </svg>
        Add Section
      </button>
      <button v-else class="btn btn-primary log-btn" @click="showModal = true" aria-label="Log a new activity">
        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round">
          <line x1="12" y1="5" x2="12" y2="19"/>
          <line x1="5" y1="12" x2="19" y2="12"/>
        </svg>
        Log Activity
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { inject, type Ref } from 'vue'
import { useRoute } from 'vue-router'

const route = useRoute()
const showModal = inject<Ref<boolean>>('showModal')!
const showAddSectionModal = inject<Ref<boolean>>('showAddSectionModal')!
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
.topbar-left { display: flex; flex-direction: column; gap: 1px; }
.page-title {
  font-family: 'Syne', sans-serif;
  font-weight: 700;
  font-size: 16px;
  color: var(--forest);
  letter-spacing: -0.01em;
  line-height: 1.1;
}
.page-sub {
  font-family: 'JetBrains Mono', monospace;
  font-size: 9px;
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
</style>
