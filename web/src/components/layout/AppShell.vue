<template>
  <div class="app-shell">
    <Sidebar />
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
</script>

<style scoped>
.app-shell {
  display: flex;
  height: 100vh;
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
</style>
