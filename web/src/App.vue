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
    <BlockModal v-if="showAddBlockModal" mode="create" @close="showAddBlockModal = false" @saved="onBlockSaved" />
  </template>
</template>

<script setup lang="ts">
import { ref, provide } from 'vue'
import { useRoute } from 'vue-router'
import { useAgroStore } from '@/stores/agro'
import AppShell from '@/components/layout/AppShell.vue'
import LogActivityModal from '@/components/modal/LogActivityModal.vue'
import BlockModal from '@/components/modal/BlockModal.vue'

const route = useRoute()
const store = useAgroStore()
const showModal = ref(false)
provide('showModal', showModal)

const showAddBlockModal = ref(false)
provide('showAddBlockModal', showAddBlockModal)

function onBlockSaved() {
  store.fetchBlocks()
}
</script>
