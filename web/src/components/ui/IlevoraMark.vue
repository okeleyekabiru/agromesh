<template>
  <!-- Ilevora mark, Direction B "Rising rows": three field rows opening upward, one in Harvest yellow. -->
  <svg :width="size" :height="size" viewBox="0 0 96 96" fill="none" role="img" aria-label="Ilevora" focusable="false">
    <rect width="96" height="96" rx="22" fill="#0D2B1F"/>
    <path d="M14 74 C34 74 52 62 82 30" stroke="#138A44" stroke-width="7" stroke-linecap="round"/>
    <path d="M14 60 C30 60 44 50 66 26" stroke="#8FD9A8" stroke-width="7" stroke-linecap="round"/>
    <path d="M14 46 C26 46 36 38 50 22" stroke="#F5C90C" stroke-width="7" stroke-linecap="round"/>
  </svg>
</template>

<script setup lang="ts">
withDefaults(defineProps<{ size?: number }>(), { size: 26 })
</script>
