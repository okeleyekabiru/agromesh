<template>
  <span class="badge" :class="`badge-${variant}`"><slot /></span>
</template>

<script setup lang="ts">
defineProps<{
  variant: 'green' | 'harvest' | 'red' | 'stone' | 'blue' | 'data'
}>()
</script>
