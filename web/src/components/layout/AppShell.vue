<template>
  <div class="app-shell">
    <!-- Slim indeterminate bar across the top of the screen while any data is loading. -->
    <Transition name="topbar-fade">
      <div v-if="store.loading" class="top-progress" role="progressbar" aria-label="Loading" aria-busy="true">
        <span class="top-progress-bar"></span>
      </div>
    </Transition>
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
import { useAgroStore } from '@/stores/agro'
import { useNavDrawer } from '@/composables/useNavDrawer'
import { useRoute } from 'vue-router'
import { watch } from 'vue'

const store = useAgroStore()
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
.drawer-backdrop { display: none; }
@media (max-width: 900px) {
  .content { padding: 14px 14px 28px; }
  .drawer-backdrop {
    display: block; position: fixed; inset: 0; z-index: 90;
    background: rgba(10, 26, 16, 0.55); backdrop-filter: blur(2px);
  }
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

.top-progress {
  position: fixed; top: 0; left: 0; right: 0; height: 3px; z-index: 1000;
  overflow: hidden; background: rgba(61, 170, 106, 0.12); pointer-events: none;
}
.top-progress-bar {
  position: absolute; top: 0; bottom: 0; width: 40%; border-radius: 0 3px 3px 0;
  background: linear-gradient(90deg, transparent, var(--palm), var(--data));
  box-shadow: 0 0 10px rgba(0, 200, 150, 0.55);
  animation: top-progress-run 1.2s var(--ease-io) infinite;
}
@keyframes top-progress-run {
  from { left: -40%; }
  to { left: 100%; }
}
.topbar-fade-enter-active, .topbar-fade-leave-active { transition: opacity var(--dur-base) var(--ease-io); }
.topbar-fade-enter-from, .topbar-fade-leave-to { opacity: 0; }
@media (prefers-reduced-motion: reduce) {
  .top-progress-bar { animation-duration: 2.4s; }
}
</style>
