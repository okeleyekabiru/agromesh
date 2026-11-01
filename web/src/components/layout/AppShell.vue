<template>
  <div class="app-shell">
    <Sidebar />
    <Transition name="topbar-fade">
      <div v-if="drawer.open.value" class="drawer-backdrop" @click="drawer.close()" />
    </Transition>
    <div class="main">
      <Topbar />
      <main class="content">
        <slot />
      </main>
    </div>
  </div>
</template>

<script setup lang="ts">
import Sidebar from './Sidebar.vue'
import Topbar from './Topbar.vue'
import { useNavDrawer } from '@/composables/useNavDrawer'
import { useRoute } from 'vue-router'
import { watch } from 'vue'

const drawer = useNavDrawer()
const route = useRoute()
watch(() => route.fullPath, () => drawer.close())
</script>

<style scoped>
.app-shell {
  display: flex;
  height: 100vh;
  height: 100dvh;
  overflow: hidden;
}
.main {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  min-width: 0;
}
.content {
  flex: 1;
  overflow-y: auto;
  padding: 20px 24px;
}
.drawer-backdrop { display: none; }
.topbar-fade-enter-active, .topbar-fade-leave-active { transition: opacity var(--dur-base) var(--ease-io); }
.topbar-fade-enter-from, .topbar-fade-leave-to { opacity: 0; }
@media (max-width: 900px) {
  .content { padding: 14px 14px 28px; }
  .drawer-backdrop {
    display: block; position: fixed; inset: 0; z-index: 90;
    background: rgba(10, 26, 16, 0.55); backdrop-filter: blur(2px);
  }
}
</style>
