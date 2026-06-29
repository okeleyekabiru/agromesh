<template>
  <div class="txn-row">
    <div class="txn-icon" :style="{ background: txn.iconBg }">{{ txn.icon }}</div>
    <div class="txn-body">
      <div class="txn-name">{{ txn.displayName }}</div>
      <div class="txn-meta">{{ txn.narration }} · {{ txn.date }}</div>
    </div>
    <div class="txn-right">
      <div v-if="showAlloc" class="txn-alloc">
        Oil Palm {{ txn.cropAllocation['Oil Palm'] || 0 }}% · Maize {{ txn.cropAllocation['Maize'] || 0 }}%
      </div>
      <div class="txn-badges">
        <StatBadge variant="stone">{{ txn.category }}</StatBadge>
        <StatBadge :variant="txn.confidence === 'high' ? 'green' : txn.confidence === 'medium' ? 'harvest' : 'red'">
          {{ txn.confidence }}
        </StatBadge>
      </div>
      <div class="txn-amount">₦{{ txn.amount.toLocaleString() }}</div>
    </div>
  </div>
</template>

<script setup lang="ts">
import StatBadge from './StatBadge.vue'
import type { Transaction } from '@/types'
defineProps<{ txn: Transaction; showAlloc?: boolean }>()
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
.txn-amount { font-family: 'JetBrains Mono', monospace; font-size: 12px; font-weight: 500; color: var(--forest); min-width: 80px; text-align: right; }
</style>
