<template>
  <div class="empty-state">
    <div class="empty-icon">{{ icon }}</div>
    <div class="empty-title">{{ title }}</div>
    <div class="empty-sub">{{ subtitle }}</div>
    <slot />
  </div>
</template>

<script setup lang="ts">
defineProps<{ icon: string; title: string; subtitle: string }>()
</script>

<style scoped>
.empty-state { text-align: center; padding: 48px 20px; }
.empty-icon { font-size: 36px; margin-bottom: 12px; opacity: 0.4; }
.empty-title { font-family: 'Syne', sans-serif; font-size: 14px; font-weight: 700; color: var(--forest); margin-bottom: 6px; }
.empty-sub { font-size: 12px; color: var(--stone); line-height: 1.6; max-width: 240px; margin: 0 auto; }
</style>
