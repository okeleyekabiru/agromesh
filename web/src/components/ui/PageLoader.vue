<template>
  <div class="pl" :class="{ 'pl-compact': compact }" role="status" aria-live="polite" aria-busy="true">
    <div class="pl-head">
      <span class="pl-ring" aria-hidden="true"></span>
      <span class="pl-label">{{ label }}</span>
    </div>
    <div v-if="!compact" class="pl-rows" aria-hidden="true">
      <div v-for="n in rows" :key="n" class="pl-row" :style="{ animationDelay: `${n * 80}ms` }">
        <span class="pl-dot"></span>
        <span class="pl-bars">
          <span class="pl-bar" :style="{ width: `${62 - (n % 3) * 12}%` }"></span>
          <span class="pl-bar pl-bar-sm" :style="{ width: `${34 + (n % 2) * 14}%` }"></span>
        </span>
        <span class="pl-chip"></span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
withDefaults(defineProps<{ label?: string; rows?: number; compact?: boolean }>(), {
  label: 'Loading…',
  rows: 4,
  compact: false
})
</script>

<style scoped>
.pl { padding: 28px 8px; }
.pl-compact { padding: 16px 8px; }
.pl-head { display: flex; align-items: center; justify-content: center; gap: 12px; margin-bottom: 22px; }
.pl-compact .pl-head { margin-bottom: 0; }
.pl-label { font-size: 13px; font-weight: 600; color: var(--stone); letter-spacing: 0.01em; }

/* Ring: a track plus a gradient arc that turns, with a soft glow. */
.pl-ring {
  width: 22px; height: 22px; border-radius: 50%; flex: none;
  background: conic-gradient(from 0deg, transparent 0 25%, var(--palm) 75%, var(--leaf) 100%);
  -webkit-mask: radial-gradient(farthest-side, transparent calc(100% - 3px), #000 calc(100% - 2.5px));
          mask: radial-gradient(farthest-side, transparent calc(100% - 3px), #000 calc(100% - 2.5px));
  animation: pl-spin 0.9s linear infinite;
  filter: drop-shadow(0 0 4px rgba(61, 170, 106, 0.35));
}

.pl-rows { display: flex; flex-direction: column; gap: 10px; max-width: 760px; margin: 0 auto; }
.pl-row {
  display: flex; align-items: center; gap: 14px; padding: 14px 16px;
  background: #fff; border: 1px solid var(--fog); border-radius: 12px; box-shadow: var(--shadow-xs);
  animation: pl-in var(--dur-enter) var(--ease-out) both;
}
.pl-bars { flex: 1; display: flex; flex-direction: column; gap: 8px; min-width: 0; }
.pl-dot, .pl-bar, .pl-chip {
  background: linear-gradient(90deg, var(--fog) 25%, var(--mist) 45%, var(--fog) 65%);
  background-size: 220% 100%;
  animation: pl-shimmer 1.4s var(--ease-io) infinite;
}
.pl-dot { width: 36px; height: 36px; border-radius: 10px; flex: none; }
.pl-bar { display: block; height: 10px; border-radius: 6px; }
.pl-bar-sm { height: 8px; }
.pl-chip { width: 64px; height: 22px; border-radius: 999px; flex: none; }

@keyframes pl-spin { to { transform: rotate(360deg); } }
@keyframes pl-shimmer { from { background-position: 120% 0; } to { background-position: -120% 0; } }
@keyframes pl-in { from { opacity: 0; transform: translateY(6px); } to { opacity: 1; transform: none; } }

@media (prefers-reduced-motion: reduce) {
  .pl-ring { animation-duration: 2.4s; }
  .pl-dot, .pl-bar, .pl-chip, .pl-row { animation: none; }
}
</style>
