<template>
  <div class="alert-banner" role="alert">
    <svg class="alert-icon" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
      <path d="M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z"/>
      <line x1="12" y1="9" x2="12" y2="13"/>
      <line x1="12" y1="17" x2="12.01" y2="17"/>
    </svg>
    <div class="alert-content"><slot /></div>
    <button v-if="action" class="alert-action" @click="$emit('action')">
      {{ action }}
      <svg width="10" height="10" viewBox="0 0 10 10" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round">
        <path d="M2 5h6M5 2l3 3-3 3"/>
      </svg>
    </button>
  </div>
</template>

<script setup lang="ts">
defineProps<{ action?: string }>()
defineEmits(['action'])
</script>

<style scoped>
.alert-icon {
  color: #B8780A;
  flex-shrink: 0;
}
.alert-content {
  flex: 1;
  font-size: 12px;
  color: var(--ink);
  line-height: 1.4;
}
.alert-action {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-family: 'Inter', sans-serif;
  font-size: 11px;
  font-weight: 600;
  color: var(--leaf);
  background: none;
  border: none;
  cursor: pointer;
  white-space: nowrap;
  padding: 0;
  transition: opacity var(--dur-fast);
}
.alert-action:hover { opacity: 0.75; }
</style>
