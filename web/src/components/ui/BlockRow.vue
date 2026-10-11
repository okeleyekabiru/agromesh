<template>
  <div class="block-row" :style="`--i: ${index ?? 0}`" @click="$emit('click')">
    <div class="s-dot" :style="{ background: dotColor }" />
    <div class="s-name">{{ block.name }}</div>
    <div class="s-crop">{{ cropLabel }}</div>
    <div class="s-bar-wrap">
      <div class="s-bar" :style="{ width: pct + '%', background: dotColor }" />
    </div>
    <div class="s-amount">₦{{ amount }}</div>
    <StatBadge
      v-if="block.status"
      :variant="block.status === 'Active' ? 'green' : block.status === 'HarvestSoon' ? 'harvest' : 'stone'"
    >
      {{ block.status }}
    </StatBadge>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import StatBadge from './StatBadge.vue'
import type { Block } from '@/types'

const props = defineProps<{
  block: Partial<Block> & { id: string; name: string }
  amount: string
  pct: number
  index?: number
}>()
defineEmits(['click'])

const COLORS = ['#3DAA6A','#E8A838','#2D7A4F','#5B8DB8','#A0783C','#8D6B94']

const dotColor = computed(() => {
  const id = props.block.id ?? ''
  return COLORS[id.charCodeAt(id.length - 1) % COLORS.length]
})

const cropLabel = computed(() =>
  props.block.crops?.map(c => c.cropType).join(' / ') ?? ''
)
</script>

<style scoped>
.block-row {
  display: flex;
  align-items: center;
  padding: 10px 16px;
  border-bottom: 1px solid var(--fog);
  gap: 12px;
  cursor: pointer;
  transition: background var(--dur-fast);
  animation: fade-up var(--dur-slow) var(--ease-out) both;
  animation-delay: calc(var(--i, 0) * 45ms + 60ms);
}
.block-row:last-child { border-bottom: none; }
.block-row:hover { background: var(--mist); }
.block-row:active { background: var(--fog); }

.s-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  flex-shrink: 0;
  transition: transform var(--dur-fast) var(--ease-spring);
}
.block-row:hover .s-dot { transform: scale(1.3); }

.s-name { font-weight: 600; font-size: 12px; color: var(--forest); flex: 1; }
.s-crop { font-family: 'JetBrains Mono', monospace; font-size: 10px; color: var(--stone); min-width: 80px; }
.s-amount { font-family: 'JetBrains Mono', monospace; font-size: 11px; font-weight: 500; color: var(--forest); min-width: 80px; text-align: right; }

.s-bar-wrap {
  width: 80px;
  height: 4px;
  background: var(--fog);
  border-radius: 2px;
  overflow: hidden;
}
.s-bar {
  height: 100%;
  border-radius: 2px;
  transform-origin: left;
  animation: bar-grow 600ms var(--ease-out) both;
  animation-delay: calc(var(--i, 0) * 60ms + 200ms);
}
</style>
