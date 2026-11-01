<template>
  <svg :width="size" :height="size" viewBox="0 0 72 72" fill="none" aria-hidden="true" focusable="false">
    <rect width="72" height="72" rx="16" fill="#1A4A32"/>
    <line x1="24" y1="14" x2="24" y2="58" stroke="#2D7A4F" stroke-width="1.5" stroke-dasharray="2 3"/>
    <line x1="48" y1="14" x2="48" y2="58" stroke="#2D7A4F" stroke-width="1.5" stroke-dasharray="2 3"/>
    <line x1="14" y1="24" x2="58" y2="24" stroke="#2D7A4F" stroke-width="1.5" stroke-dasharray="2 3"/>
    <line x1="14" y1="48" x2="58" y2="48" stroke="#2D7A4F" stroke-width="1.5" stroke-dasharray="2 3"/>
    <circle cx="24" cy="24" r="3.5" fill="#3DAA6A"/>
    <circle cx="48" cy="24" r="3.5" fill="#3DAA6A"/>
    <circle cx="24" cy="48" r="3.5" fill="#3DAA6A"/>
    <circle cx="48" cy="48" r="3.5" fill="#3DAA6A"/>
    <circle cx="36" cy="36" r="7" fill="#E8A838"/>
    <circle cx="36" cy="36" r="3.5" fill="#0D2B1F"/>
    <line x1="24" y1="24" x2="36" y2="36" stroke="#3DAA6A" stroke-width="1.5"/>
    <line x1="48" y1="24" x2="36" y2="36" stroke="#3DAA6A" stroke-width="1.5"/>
    <line x1="24" y1="48" x2="36" y2="36" stroke="#3DAA6A" stroke-width="1.5"/>
    <line x1="48" y1="48" x2="36" y2="36" stroke="#3DAA6A" stroke-width="1.5"/>
  </svg>
</template>

<script setup lang="ts">
withDefaults(defineProps<{ size?: number }>(), { size: 26 })
</script>
