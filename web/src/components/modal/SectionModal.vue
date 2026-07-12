<template>
  <div class="modal-overlay" @click.self="$emit('close')">
    <div class="modal-wrap">
      <button class="modal-close" @click="$emit('close')">×</button>
      <SectionBoundaryEditor :mode="mode" :section="section" @saved="onSaved" @cancelled="$emit('close')" />
    </div>
  </div>
</template>

<script setup lang="ts">
import SectionBoundaryEditor from '@/components/section/SectionBoundaryEditor.vue'
import type { Section } from '@/types'

defineProps<{ mode: 'create' | 'edit'; section?: Section | null }>()
const emit = defineEmits<{ close: []; saved: [section: Section] }>()

function onSaved(section: Section) {
  emit('saved', section)
  emit('close')
}
</script>

<style scoped>
.modal-overlay {
  position: fixed; inset: 0; background: rgba(10,26,16,0.6); backdrop-filter: blur(4px);
  z-index: 200; display: flex; align-items: flex-start; justify-content: center;
  padding: 48px 24px; overflow-y: auto;
}
/* No overflow/max-height here — the button is positioned relative to this element, and an
   overflow container would clip it since it sits outside the content's own box. Scrolling for
   tall content is handled by .modal-overlay itself instead. */
.modal-wrap { position: relative; }
.modal-close {
  position: absolute; top: -14px; right: -14px; width: 32px; height: 32px; border-radius: 50%;
  border: none; background: #fff; box-shadow: 0 4px 12px rgba(0,0,0,0.2); cursor: pointer; font-size: 18px; color: #6b7280; z-index: 1;
}
</style>
