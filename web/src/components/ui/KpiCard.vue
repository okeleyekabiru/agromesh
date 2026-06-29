<template>
  <div class="kpi-card" :class="{ dark }">
    <div class="kpi-label">{{ label }}</div>
    <div class="kpi-value">{{ value }}</div>
    <div class="kpi-delta" :class="`delta-${deltaType}`">{{ delta }}</div>
    <div v-if="dark" class="kpi-accent" />
  </div>
</template>

<script setup lang="ts">
defineProps<{
  label: string
  value: string
  delta: string
  deltaType: 'up' | 'down' | 'warn' | 'mute'
  dark?: boolean
}>()
</script>

<style scoped>
.kpi-card { background: white; border: 1px solid var(--fog); border-radius: 12px; padding: 16px; position: relative; overflow: hidden; }
.kpi-card.dark { background: var(--forest); border-color: var(--canopy); }
.kpi-label { font-family: 'JetBrains Mono', monospace; font-size: 9px; letter-spacing: 0.12em; text-transform: uppercase; color: var(--stone); margin-bottom: 8px; }
.kpi-card.dark .kpi-label { color: rgba(107,124,114,0.8); }
.kpi-value { font-family: 'Syne', sans-serif; font-weight: 700; font-size: 26px; color: var(--forest); letter-spacing: -0.02em; line-height: 1; }
.kpi-card.dark .kpi-value { color: white; }
.kpi-delta { font-size: 11px; font-weight: 500; margin-top: 4px; }
.delta-up   { color: var(--palm); }
.delta-down { color: var(--alert); }
.delta-warn { color: var(--harvest); }
.delta-mute { color: var(--stone); }
.kpi-accent { position: absolute; bottom: 0; right: 0; width: 48px; height: 48px; border-radius: 50%; background: var(--palm); opacity: 0.06; transform: translate(12px, 12px); }
</style>
