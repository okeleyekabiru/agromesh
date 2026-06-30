<template>
  <template v-if="route.meta.public">
    <RouterView />
  </template>
  <template v-else>
    <AppShell>
      <RouterView v-slot="{ Component, route: r }">
        <Transition name="page" mode="out-in">
          <component :is="Component" :key="r.path" />
        </Transition>
      </RouterView>
    </AppShell>
    <LogActivityModal v-if="showModal" @close="showModal = false" />
  </template>
</template>

<script setup lang="ts">
import { ref, provide } from 'vue'
import { useRoute } from 'vue-router'
import AppShell from '@/components/layout/AppShell.vue'
import LogActivityModal from '@/components/modal/LogActivityModal.vue'

const route = useRoute()
const showModal = ref(false)
provide('showModal', showModal)
</script>
