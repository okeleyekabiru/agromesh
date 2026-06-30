<template>
  <div class="activity-row" :style="`--i: ${index ?? 0}`">
    <div class="act-icon" :style="{ background: iconBg }">
      <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor"
           stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"
           :style="{ color: iconColor }"
           v-html="iconPath" />
    </div>
    <div class="act-body">
      <div class="act-title">{{ activity.activityType }}</div>
      <div class="act-detail">
        {{ sectionLabel }} · {{ activity.workers.length }} worker{{ activity.workers.length !== 1 ? 's' : '' }}
        <span v-if="activity.inputs.length"> · {{ activity.inputs[0].quantity }}{{ activity.inputs[0].unit }} {{ activity.inputs[0].item }}</span>
        <span v-if="activity.chemicalProductName"> · {{ activity.chemicalProductName }}</span>
      </div>
    </div>
    <div class="act-right">
      <div class="act-cost">₦{{ totalCost.toLocaleString() }}</div>
      <div class="act-time">{{ formatDate(activity.date) }}</div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useAgroStore } from '@/stores/agro'
import type { ActivityLog } from '@/types'

const props = defineProps<{ activity: ActivityLog; index?: number }>()
const store = useAgroStore()

const ICON_PATHS: Record<string, string> = {
  Weeding:               '<path d="M12 22V12M12 12C12 7 7 4 3 5c2 4 5 7 9 7z"/><path d="M12 12c0-5 5-8 9-7-2 4-5 7-9 7z"/>',
  Spraying:              '<path d="M12 2.69l5.66 5.66a8 8 0 11-11.31 0L12 2.69z"/>',
  FertilizerApplication: '<path d="M5 8h14l-1.5 8H6.5L5 8z"/><path d="M8 8V6a4 4 0 018 0v2"/><circle cx="12" cy="13" r="1.5" fill="currentColor"/>',
  Maintenance:           '<path d="M14.7 6.3a1 1 0 000 1.4l1.6 1.6a1 1 0 001.4 0l3.77-3.77a6 6 0 01-7.94 7.94l-6.91 6.91a2.12 2.12 0 01-3-3l6.91-6.91a6 6 0 017.94-7.94l-3.76 3.76z"/>',
  Harvesting:            '<path d="M3 17l4-8 4 4 4-8 4 8M3 21h18M12 3v4"/>',
  Clearing:              '<circle cx="6" cy="6" r="3"/><circle cx="6" cy="18" r="3"/><path d="M20 4L8.12 15.88M14.47 14.48L20 20M8.12 8.12L12 12"/>',
  Pruning:               '<line x1="4" y1="20" x2="20" y2="4"/><path d="M8.5 5.5L3 11l1.5 1.5M15.5 18.5L21 13l-1.5-1.5"/>',
  PestControl:           '<path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>',
}

const ICON_STYLES: Record<string, [string, string]> = {
  Weeding:               ['rgba(45,122,79,0.12)',   'var(--leaf)'],
  Spraying:              ['rgba(59,130,246,0.12)',   '#2563EB'],
  FertilizerApplication: ['rgba(61,170,106,0.12)',   'var(--palm)'],
  Maintenance:           ['rgba(160,120,60,0.12)',   '#9A7040'],
  Harvesting:            ['rgba(232,168,56,0.12)',   '#B8780A'],
  Clearing:              ['rgba(107,124,114,0.12)',  'var(--stone)'],
  Pruning:               ['rgba(45,122,79,0.10)',    'var(--leaf)'],
  PestControl:           ['rgba(220,60,60,0.10)',    'var(--alert)'],
}

const iconPath   = computed(() => ICON_PATHS[props.activity.activityType] ?? '<rect x="3" y="3" width="18" height="18" rx="2"/>')
const iconBg     = computed(() => (ICON_STYLES[props.activity.activityType] ?? ['rgba(0,0,0,0.06)', 'var(--stone)'])[0])
const iconColor  = computed(() => (ICON_STYLES[props.activity.activityType] ?? ['', 'var(--stone)'])[1])
const totalCost  = computed(() => (props.activity.labourCost ?? 0) + (props.activity.inputCost ?? 0))

const sectionLabel = computed(() => {
  const sec = store.sections.find(s => s.id === props.activity.sectionId)
  return sec?.name ?? props.activity.sectionId
})

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-NG', { day: 'numeric', month: 'short' })
}
</script>

<style scoped>
.activity-row {
  display: flex;
  align-items: flex-start;
  padding: 11px 16px;
  border-bottom: 1px solid var(--fog);
  gap: 12px;
  transition: background var(--dur-fast);
  animation: fade-up var(--dur-slow) var(--ease-out) both;
  animation-delay: calc(var(--i, 0) * 45ms + 60ms);
}
.activity-row:last-child { border-bottom: none; }
.activity-row:hover { background: var(--mist); }

.act-icon {
  width: 30px;
  height: 30px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  margin-top: 1px;
  transition: transform var(--dur-fast) var(--ease-spring);
}
.activity-row:hover .act-icon { transform: scale(1.1); }

.act-body { flex: 1; min-width: 0; }
.act-title { font-size: 12px; font-weight: 600; color: var(--forest); }
.act-detail {
  font-family: 'JetBrains Mono', monospace;
  font-size: 9px;
  color: var(--stone);
  margin-top: 2px;
  line-height: 1.5;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.act-right { text-align: right; flex-shrink: 0; }
.act-cost {
  font-family: 'Syne', sans-serif;
  font-weight: 700;
  font-size: 13px;
  color: var(--forest);
}
.act-time {
  font-family: 'JetBrains Mono', monospace;
  font-size: 9px;
  color: var(--stone);
  margin-top: 2px;
}
</style>
