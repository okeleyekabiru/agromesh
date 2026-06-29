<template>
  <AppShell>
    <RouterView />
  </AppShell>
  <LogActivityModal v-if="showModal" @close="showModal = false" />
</template>

<script setup lang="ts">
import { ref, provide } from 'vue'
import AppShell from '@/components/layout/AppShell.vue'
import LogActivityModal from '@/components/modal/LogActivityModal.vue'

const showModal = ref(false)
provide('showModal', showModal)
</script>
