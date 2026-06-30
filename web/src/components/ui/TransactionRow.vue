<template>
  <div class="txn-row">
    <div class="txn-icon" :style="{ background: categoryColor }">{{ categoryIcon }}</div>
    <div class="txn-body">
      <div class="txn-name">{{ txn.narration }}</div>
      <div class="txn-meta">{{ formatDate(txn.date) }} · Source: {{ txn.source }}</div>
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
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import StatBadge from './StatBadge.vue'
import type { Transaction } from '@/types'

const props = defineProps<{ txn: Transaction; showAlloc?: boolean }>()

const CAT_ICONS: Record<string, string> = {
  Labour: '👷', Fertilizer: '🌱', Chemicals: '🧪', Fuel: '⛽',
  Equipment: '🔧', Maintenance: '🛠️', Transportation: '🚚',
  Seeds: '🌾', Sales: '💰', Other: '📋'
}
const CAT_COLORS: Record<string, string> = {
  Labour: 'rgba(61,170,106,0.12)', Fertilizer: 'rgba(45,122,79,0.12)',
  Chemicals: 'rgba(91,141,184,0.15)', Fuel: 'rgba(232,168,56,0.15)',
  Equipment: 'rgba(160,120,60,0.12)', Sales: 'rgba(61,170,106,0.2)',
  Other: 'rgba(0,0,0,0.06)'
}

const categoryIcon = computed(() => CAT_ICONS[props.txn.category] ?? '📋')
const categoryColor = computed(() => CAT_COLORS[props.txn.category] ?? 'rgba(0,0,0,0.06)')

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-NG', { day: 'numeric', month: 'short', year: '2-digit' })
}
</script>

<style scoped>
.txn-row { display: flex; align-items: center; padding: 10px 16px; border-bottom: 1px solid var(--fog); gap: 12px; }
.txn-row:last-child { border-bottom: none; }
.txn-icon { width: 32px; height: 32px; border-radius: 8px; display: flex; align-items: center; justify-content: center; flex-shrink: 0; font-size: 14px; }
.txn-body { flex: 1; min-width: 0; }
.txn-name { font-size: 12px; font-weight: 600; color: var(--forest); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.txn-meta { font-family: 'JetBrains Mono', monospace; font-size: 9px; color: var(--stone); margin-top: 1px; }
.txn-right { display: flex; align-items: center; gap: 10px; flex-shrink: 0; }
.txn-alloc { font-family: 'JetBrains Mono', monospace; font-size: 10px; color: var(--stone); }
.txn-badges { display: flex; gap: 6px; }
.txn-amount { font-family: 'Syne', sans-serif; font-weight: 700; font-size: 13px; color: var(--forest); min-width: 80px; text-align: right; }
</style>
