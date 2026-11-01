<template>
  <div class="settings">
    <FarmLogoCard />
    <BusinessProfileCard />
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useAgroStore } from '@/stores/agro'
import FarmLogoCard from '@/components/settings/FarmLogoCard.vue'
import BusinessProfileCard from '@/components/settings/BusinessProfileCard.vue'

const store = useAgroStore()
// The settings are about the active farm, so make sure its record (hasLogo, logoUpdatedAt) is loaded
// even when the user opened this page directly.
onMounted(() => { if (!store.farm) store.fetchFarm() })
</script>

<style scoped>
.settings { max-width: 820px; }
</style>
