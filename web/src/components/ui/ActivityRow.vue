<template>
  <div class="activity-row">
    <div class="act-icon">{{ activity.icon }}</div>
    <div class="act-body">
      <div class="act-title">{{ activity.activityType }}</div>
      <div class="act-detail">
        {{ activity.sectionName }} · {{ activity.blockName }} ·
        {{ activity.workers.length }} workers
        <span v-if="activity.inputs.length"> · {{ activity.inputs[0].quantity }}{{ activity.inputs[0].unit }} {{ activity.inputs[0].item }}</span>
      </div>
    </div>
    <div class="act-right">
      <div class="act-cost">₦{{ activity.totalCost.toLocaleString() }}</div>
      <div class="act-time">{{ activity.date }}</div>
      <StatBadge variant="green" style="margin-top:3px;display:inline-block;">{{ activity.supervisorName }}</StatBadge>
    </div>
  </div>
</template>

<script setup lang="ts">
import StatBadge from './StatBadge.vue'
import type { ActivityLog } from '@/types'
defineProps<{ activity: ActivityLog }>()
</script>

<style scoped>
.activity-row { display: flex; align-items: flex-start; padding: 10px 16px; border-bottom: 1px solid var(--fog); gap: 12px; }
.activity-row:last-child { border-bottom: none; }
.act-icon { width: 28px; height: 28px; border-radius: 6px; background: rgba(45,122,79,0.1); display: flex; align-items: center; justify-content: center; flex-shrink: 0; margin-top: 1px; font-size: 14px; }
.act-body { flex: 1; }
.act-title { font-size: 12px; font-weight: 600; color: var(--forest); }
.act-detail { font-family: 'JetBrains Mono', monospace; font-size: 9px; color: var(--stone); margin-top: 2px; line-height: 1.5; }
.act-right { text-align: right; flex-shrink: 0; }
.act-cost { font-family: 'Syne', sans-serif; font-weight: 700; font-size: 13px; color: var(--forest); }
.act-time { font-family: 'JetBrains Mono', monospace; font-size: 9px; color: var(--stone); margin-top: 2px; }
</style>
