<template>
  <div class="section-row" @click="$emit('click')">
    <div class="s-dot" :style="{ background: section.color }" />
    <div class="s-name">{{ section.name }}</div>
    <div class="s-crop">{{ section.crop }}</div>
    <div class="s-bar-wrap">
      <div class="s-bar" :style="{ width: pct + '%', background: section.color }" />
    </div>
    <div class="s-amount">₦{{ amount }}</div>
    <StatBadge :variant="section.status === 'Active' ? 'green' : section.status === 'Harvest Soon' ? 'harvest' : 'stone'">
      {{ section.status }}
    </StatBadge>
  </div>
</template>

<script setup lang="ts">
import StatBadge from './StatBadge.vue'
import type { Section } from '@/types'
defineProps<{ section: Section; amount: string; pct: number }>()
defineEmits(['click'])
</script>

<style scoped>
.section-row { display: flex; align-items: center; padding: 10px 16px; border-bottom: 1px solid var(--fog); gap: 12px; cursor: pointer; transition: background 0.1s; }
.section-row:last-child { border-bottom: none; }
.section-row:hover { background: var(--mist); }
.s-dot { width: 8px; height: 8px; border-radius: 50%; flex-shrink: 0; }
.s-name { font-weight: 600; font-size: 12px; color: var(--forest); flex: 1; }
.s-crop { font-family: 'JetBrains Mono', monospace; font-size: 10px; color: var(--stone); min-width: 70px; }
.s-amount { font-family: 'JetBrains Mono', monospace; font-size: 11px; font-weight: 500; color: var(--forest); min-width: 80px; text-align: right; }
.s-bar-wrap { width: 80px; height: 4px; background: var(--fog); border-radius: 2px; overflow: hidden; }
.s-bar { height: 100%; border-radius: 2px; }
</style>
