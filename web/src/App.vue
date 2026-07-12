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
    <SectionModal v-if="showAddSectionModal" mode="create" @close="showAddSectionModal = false" @saved="onSectionSaved" />
  </template>
</template>

<script setup lang="ts">
import { ref, provide } from 'vue'
import { useRoute } from 'vue-router'
import { useAgroStore } from '@/stores/agro'
import AppShell from '@/components/layout/AppShell.vue'
import LogActivityModal from '@/components/modal/LogActivityModal.vue'
import SectionModal from '@/components/modal/SectionModal.vue'

const route = useRoute()
const store = useAgroStore()
const showModal = ref(false)
provide('showModal', showModal)

const showAddSectionModal = ref(false)
provide('showAddSectionModal', showAddSectionModal)

function onSectionSaved() {
  store.fetchSections()
}
</script>
