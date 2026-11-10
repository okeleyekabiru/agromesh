<template>
  <div class="shell">
    <div class="card-box">
      <div class="brand">Ilevora</div>
      <h1>{{ title }}</h1>
      <p v-if="subtitle" class="sub">{{ subtitle }}</p>
      <slot />
    </div>
  </div>
</template>

<script setup lang="ts">
defineProps<{ title: string; subtitle?: string }>()
</script>

<style scoped>
.shell { min-height: 100vh; display: flex; align-items: center; justify-content: center; background: var(--mist, #f4f7f5); padding: 24px 16px; }
.card-box { width: 100%; max-width: 420px; background: #fff; border-radius: 14px; padding: 28px; box-shadow: 0 8px 30px rgba(13, 43, 31, 0.08); }
.brand { font-weight: 800; font-size: 20px; color: #138A44; margin-bottom: 18px; }
h1 { font-size: 22px; margin: 0 0 6px; color: var(--forest, #0d2b1f); }
.sub { font-size: 13px; color: var(--stone, #5b6b63); margin: 0 0 18px; line-height: 1.5; }
</style>
