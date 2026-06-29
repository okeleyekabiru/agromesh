<template>
  <button class="filter-pill" :class="{ active }" @click="$emit('click')">
    <slot />
  </button>
</template>

<script setup lang="ts">
defineProps<{ active?: boolean }>()
defineEmits(['click'])
</script>
