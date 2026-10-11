<template>
  <div class="txn-row" :class="{ clickable: clickable }" :style="`--i: ${index ?? 0}`"
       :role="clickable ? 'button' : undefined" :tabindex="clickable ? 0 : undefined"
       @click="clickable && emit('open')" @keydown.enter="clickable && emit('open')">
    <div class="txn-icon" :style="{ background: categoryBg }">
      <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor"
           stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"
           :style="{ color: categoryColor }"
           v-html="categoryIconPath" />
    </div>
    <div class="txn-body">
      <div class="txn-name">{{ txn.narration }}</div>
      <div class="txn-meta">{{ formatDate(txn.date) }} · {{ txn.source }}</div>
    </div>
    <div class="txn-right">
      <div v-if="showAlloc && txn.cropAllocation" class="txn-alloc">
        <span v-for="(pct, crop) in txn.cropAllocation" :key="crop">{{ crop }} {{ pct }}% </span>
      </div>
      <div class="txn-badges">
        <StatBadge variant="stone">{{ txn.category }}</StatBadge>
        <StatBadge :variant="txn.confidence === 'High' ? 'green' : txn.confidence === 'Medium' ? 'harvest' : 'red'">
          {{ txn.confidence }}
        </StatBadge>
        <StatBadge v-if="txn.reviewedByUser" variant="green">Reviewed</StatBadge>
      </div>
      <div class="txn-amount">₦{{ txn.amount.toLocaleString() }}</div>
    </div>
    <div v-if="$slots.actions" class="txn-actions"><slot name="actions" /></div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import StatBadge from './StatBadge.vue'
import type { Transaction } from '@/types'

const props = defineProps<{ txn: Transaction; showAlloc?: boolean; index?: number; clickable?: boolean }>()
const emit = defineEmits<{ open: [] }>()

const CAT_ICONS: Record<string, string> = {
  Labour:         '<path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2"/><circle cx="9" cy="7" r="4"/>',
  Fertilizer:     '<path d="M12 22V12M12 12C12 7 7 4 3 5c2 4 5 7 9 7z"/><path d="M12 12c0-5 5-8 9-7-2 4-5 7-9 7z"/>',
  Chemicals:      '<path d="M9 3h6l1 8H8L9 3z"/><path d="M4 21h16M8 11l-2 10h12l-2-10"/>',
  Fuel:           '<path d="M3 22V5a2 2 0 012-2h8a2 2 0 012 2v3"/><path d="M15 8h2a2 2 0 012 2v9a1 1 0 01-1 1h-2M5 22h10"/>',
  Equipment:      '<path d="M14.7 6.3a1 1 0 000 1.4l1.6 1.6a1 1 0 001.4 0l3.77-3.77a6 6 0 01-7.94 7.94l-6.91 6.91a2.12 2.12 0 01-3-3l6.91-6.91a6 6 0 017.94-7.94l-3.76 3.76z"/>',
  Maintenance:    '<circle cx="12" cy="12" r="3"/><path d="M19.07 4.93l-1.41 1.41M5.34 18.66l-1.41 1.41M22 12h-2M4 12H2M19.07 19.07l-1.41-1.41M5.34 5.34L3.93 3.93"/>',
  Transportation: '<rect x="1" y="3" width="15" height="13" rx="2"/><path d="M16 8h4l3 5v3h-7V8z"/><circle cx="5.5" cy="18.5" r="2.5"/><circle cx="18.5" cy="18.5" r="2.5"/>',
  Seeds:          '<path d="M3 17l4-8 4 4 4-8 4 8M3 21h18M12 3v4"/>',
  Sales:          '<line x1="12" y1="1" x2="12" y2="23"/><path d="M17 5H9.5a3.5 3.5 0 000 7h5a3.5 3.5 0 010 7H6"/>',
  Other:          '<circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/>',
}

const CAT_COLORS: Record<string, [string, string]> = {
  Labour:         ['rgba(61,170,106,0.12)',  'var(--palm)'],
  Fertilizer:     ['rgba(45,122,79,0.12)',   'var(--leaf)'],
  Chemicals:      ['rgba(59,130,246,0.12)',  '#2563EB'],
  Fuel:           ['rgba(232,168,56,0.14)',  '#B8780A'],
  Equipment:      ['rgba(160,120,60,0.12)',  '#9A7040'],
  Maintenance:    ['rgba(107,124,114,0.12)', 'var(--stone)'],
  Transportation: ['rgba(99,102,241,0.12)',  '#6366F1'],
  Seeds:          ['rgba(232,168,56,0.10)',  '#B8780A'],
  Sales:          ['rgba(0,200,150,0.12)',   'var(--data)'],
  Other:          ['rgba(0,0,0,0.06)',       'var(--stone)'],
}

const categoryIconPath = computed(() => CAT_ICONS[props.txn.category] ?? CAT_ICONS.Other)
const categoryBg       = computed(() => (CAT_COLORS[props.txn.category] ?? CAT_COLORS.Other)[0])
const categoryColor    = computed(() => (CAT_COLORS[props.txn.category] ?? CAT_COLORS.Other)[1])

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-NG', { day: 'numeric', month: 'short', year: '2-digit' })
}
</script>

<style scoped>
.txn-row {
  display: flex;
  align-items: center;
  padding: 11px 16px;
  border-bottom: 1px solid var(--fog);
  gap: 12px;
  transition: background var(--dur-fast);
  animation: fade-up var(--dur-slow) var(--ease-out) both;
  animation-delay: calc(var(--i, 0) * 45ms + 60ms);
}
.txn-row:last-child { border-bottom: none; }
.txn-row:hover { background: var(--mist); }

.txn-icon {
  width: 32px;
  height: 32px;
  border-radius: 9px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  transition: transform var(--dur-fast) var(--ease-spring);
}
.txn-row:hover .txn-icon { transform: scale(1.1); }

.txn-body { flex: 1; min-width: 0; }
.txn-name {
  font-size: 12px;
  font-weight: 600;
  color: var(--forest);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.txn-meta {
  font-family: 'JetBrains Mono', monospace;
  font-size: 9px;
  color: var(--stone);
  margin-top: 2px;
}

.txn-row.clickable { cursor: pointer; }
.txn-row.clickable:hover { background: rgba(45,122,79,0.04); }
.txn-actions { flex-basis: 100%; padding: 8px 0 0 40px; display: flex; gap: 8px; align-items: center; flex-wrap: wrap; }
.txn-right { display: flex; align-items: center; gap: 10px; flex-shrink: 0; }
.txn-alloc { font-family: 'JetBrains Mono', monospace; font-size: 10px; color: var(--stone); }
.txn-badges { display: flex; gap: 6px; }
.txn-amount {
  font-family: 'Syne', sans-serif;
  font-weight: 700;
  font-size: 13px;
  color: var(--forest);
  min-width: 80px;
  text-align: right;
}
</style>
