<template>
  <div class="alert-banner">
    <span style="font-size:16px;flex-shrink:0;">⚠</span>
    <div style="flex:1;font-size:12px;color:var(--ink);"><slot /></div>
    <button v-if="action" class="alert-action" @click="$emit('action')">{{ action }} →</button>
  </div>
</template>

<script setup lang="ts">
defineProps<{ action?: string }>()
defineEmits(['action'])
</script>

<style scoped>
.alert-action {
  font-family: 'Inter', sans-serif;
  font-size: 11px;
  font-weight: 600;
  color: var(--leaf);
  background: none;
  border: none;
  cursor: pointer;
  white-space: nowrap;
}
</style>
