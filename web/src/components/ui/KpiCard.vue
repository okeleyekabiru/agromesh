<template>
  <div class="kpi-card" :class="{ dark }">
    <div class="kpi-inner">
      <div class="kpi-top-row">
        <div class="kpi-label">{{ label }}</div>
        <slot name="icon" />
      </div>
      <div class="kpi-value">{{ value }}</div>
      <div class="kpi-delta" :class="`delta-${deltaType}`">
        <svg v-if="deltaType === 'up'" width="9" height="9" viewBox="0 0 9 9" fill="none">
          <path d="M4.5 7.5V2M2 4.5l2.5-2.5 2.5 2.5" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>
        <svg v-else-if="deltaType === 'down'" width="9" height="9" viewBox="0 0 9 9" fill="none">
          <path d="M4.5 1.5v5.5M2 4.5l2.5 2.5 2.5-2.5" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/>
        </svg>
        <svg v-else-if="deltaType === 'warn'" width="9" height="9" viewBox="0 0 9 9" fill="none">
          <path d="M4.5 1L1 8h7L4.5 1z" stroke="currentColor" stroke-width="1.3" stroke-linecap="round" stroke-linejoin="round"/>
          <path d="M4.5 4v2M4.5 6.5v.2" stroke="currentColor" stroke-width="1.2" stroke-linecap="round"/>
        </svg>
        {{ delta }}
      </div>
      <slot name="sparkline" />
    </div>
    <div v-if="dark" class="kpi-glow" />
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
.kpi-card {
  background: white;
  border: 1px solid var(--fog);
  border-radius: 14px;
  padding: 18px 20px;
  position: relative;
  overflow: hidden;
  transition:
    box-shadow var(--dur-base) var(--ease-out),
    transform  var(--dur-base) var(--ease-out),
    border-color var(--dur-base);
  cursor: default;
  box-shadow: var(--shadow-xs);
}

.kpi-card:hover {
  box-shadow: var(--shadow-md);
  transform: translateY(-2px);
  border-color: rgba(45,122,79,0.18);
}

.kpi-card:active { transform: translateY(0); box-shadow: var(--shadow-xs); }

.kpi-card.dark {
  background: var(--canopy);
  border-color: transparent;
  box-shadow: var(--shadow-sm);
}
.kpi-card.dark:hover {
  border-color: rgba(61,170,106,0.25);
  box-shadow: 0 6px 20px rgba(10,26,16,0.25);
}

.kpi-inner { position: relative; z-index: 1; }

.kpi-top-row {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 6px;
}

.kpi-label {
  font-family: 'JetBrains Mono', monospace;
  font-size: 9px;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  color: var(--stone);
}
.kpi-card.dark .kpi-label { color: rgba(107,124,114,0.75); }

.kpi-value {
  font-family: 'Syne', sans-serif;
  font-weight: 700;
  font-size: 26px;
  color: var(--forest);
  letter-spacing: -0.02em;
  line-height: 1;
  margin-bottom: 5px;
}
.kpi-card.dark .kpi-value { color: white; }

.kpi-delta {
  display: flex;
  align-items: center;
  gap: 3px;
  font-size: 11px;
  font-weight: 500;
}
.delta-up   { color: var(--palm); }
.delta-down { color: var(--alert); }
.delta-warn { color: var(--harvest); }
.delta-mute { color: var(--stone); }

.kpi-card.dark .delta-mute { color: rgba(107,124,114,0.7); }

/* Decorative glow for dark card */
.kpi-glow {
  position: absolute;
  bottom: -16px;
  right: -16px;
  width: 64px;
  height: 64px;
  border-radius: 50%;
  background: var(--palm);
  opacity: 0.07;
  pointer-events: none;
}
</style>
