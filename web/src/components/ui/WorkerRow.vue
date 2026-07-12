<template>
  <div class="worker-row">
    <div class="w-avatar">{{ worker.initials }}</div>
    <div class="w-info">
      <div class="w-name">{{ worker.name }}</div>
      <div class="w-role">{{ worker.roleTitle }}</div>
    </div>
    <StatBadge :variant="worker.type === 'Employee' ? 'green' : 'blue'">{{ worker.type }}</StatBadge>
    <div class="w-stats">
      <div class="w-days">{{ worker.daysLoggedThisMonth }} days</div>
      <div class="w-pay">₦{{ worker.payRate.toLocaleString() }} / {{ PAY_FREQUENCY_LABELS[worker.payFrequency].toLowerCase() }}</div>
    </div>
  </div>
</template>

<script setup lang="ts">
import StatBadge from './StatBadge.vue'
import type { Worker } from '@/types'
import { PAY_FREQUENCY_LABELS } from '@/types'
defineProps<{ worker: Worker }>()
</script>

<style scoped>
.worker-row { display: flex; align-items: center; padding: 12px 16px; border-bottom: 1px solid var(--fog); gap: 12px; }
.worker-row:last-child { border-bottom: none; }
.w-avatar { width: 34px; height: 34px; border-radius: 50%; background: var(--canopy); display: flex; align-items: center; justify-content: center; font-family: 'Syne', sans-serif; font-weight: 700; font-size: 13px; color: var(--palm); flex-shrink: 0; }
.w-info { flex: 1; }
.w-name { font-size: 12px; font-weight: 600; color: var(--forest); }
.w-role { font-family: 'JetBrains Mono', monospace; font-size: 9px; color: var(--stone); margin-top: 1px; }
.w-stats { text-align: right; }
.w-days { font-family: 'Syne', sans-serif; font-weight: 700; font-size: 14px; color: var(--forest); }
.w-pay { font-family: 'JetBrains Mono', monospace; font-size: 10px; color: var(--stone); }
</style>
