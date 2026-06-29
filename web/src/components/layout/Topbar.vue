<template>
  <div class="topbar">
    <div>
      <div class="page-title">{{ route.meta.title as string }}</div>
      <div class="page-sub">{{ route.meta.sub as string }}</div>
    </div>
    <div class="topbar-right">
      <button v-if="route.name === 'finance'" class="btn btn-ghost btn-sm">
        <DownloadIcon /> Export CSV
      </button>
      <button class="btn btn-primary" @click="showModal = true">
        <PlusIcon /> Log Activity
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { inject, type Ref } from 'vue'
import { useRoute } from 'vue-router'

const route = useRoute()
const showModal = inject<Ref<boolean>>('showModal')!

const PlusIcon = { template: `<svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/></svg>` }
const DownloadIcon = { template: `<svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4M7 10l5 5 5-5M12 15V3"/></svg>` }
</script>

<style scoped>
.topbar {
  background: white;
  border-bottom: 1px solid var(--fog);
  padding: 0 24px;
  height: 52px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-shrink: 0;
}
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
  letter-spacing: 0.1em;
  text-transform: uppercase;
  margin-top: 1px;
}
.topbar-right { display: flex; align-items: center; gap: 10px; }
</style>
