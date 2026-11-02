<template>
  <img
    v-if="url"
    v-show="!broken"
    :src="url"
    :alt="farmName ? `${farmName} logo` : 'Farm logo'"
    class="farm-logo"
    :class="`farm-logo--${variant}`"
    @error="broken = true"
  />
  <!-- No logo, still loading, or it failed to load: the caller's fallback, Ilevora by default. -->
  <slot v-if="!url || broken" name="fallback">
    <IlevoraMark :size="56" />
  </slot>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { useFarmLogo } from '@/composables/useFarmLogo'
import IlevoraMark from '@/components/ui/IlevoraMark.vue'

// The one place a farm's logo is drawn in the app (the settings preview; invoices print it server-side). It is never
// used in place of the Ilevora logo. It fetches the bytes with the bearer token and revokes its object URL when the
// logo changes or the component goes away.
const props = withDefaults(defineProps<{
  farmId?: string | null
  hasLogo?: boolean | null
  /** farm.logoUpdatedAt, used to bust the cache when the logo is replaced. */
  version?: string | null
  farmName?: string | null
  variant?: 'preview'
}>(), { variant: 'preview' })

const { url } = useFarmLogo(() => ({ farmId: props.farmId, hasLogo: props.hasLogo, version: props.version }))

// A 200 response that is not a decodable image: show the fallback instead of an empty box.
const broken = ref(false)
watch(url, () => { broken.value = false })
</script>

<style scoped>
.farm-logo { display: block; object-fit: contain; }
.farm-logo--preview {
  max-width: 100%;
  max-height: 120px;
}
</style>
