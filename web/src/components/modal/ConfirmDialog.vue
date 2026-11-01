<template>
  <div class="cd-overlay" @click.self="$emit('cancel')" @keydown.esc.stop="$emit('cancel')">
    <div
      ref="dialog"
      class="cd-box"
      role="alertdialog"
      aria-modal="true"
      :aria-labelledby="titleId"
      :aria-describedby="descId"
      @keydown.tab.stop="trapFocus"
    >
      <h2 :id="titleId" class="cd-title">{{ title }}</h2>
      <p :id="descId" class="cd-message">{{ message }}</p>
      <div v-if="error" class="cd-error" role="alert">{{ error }}</div>
      <div class="cd-actions">
        <button ref="cancelBtn" type="button" class="btn btn-ghost" :disabled="busy" @click="$emit('cancel')">Cancel</button>
        <button type="button" class="btn cd-confirm" :disabled="busy" @click="$emit('confirm')">
          {{ busy ? 'Working…' : confirmLabel }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue'

withDefaults(defineProps<{
  title: string
  message: string
  confirmLabel?: string
  busy?: boolean
  error?: string
}>(), { confirmLabel: 'Delete', busy: false, error: '' })
defineEmits<{ confirm: []; cancel: [] }>()

const uid = Math.random().toString(36).slice(2, 8)
const titleId = `cd-title-${uid}`
const descId = `cd-desc-${uid}`
const dialog = ref<HTMLElement | null>(null)
const cancelBtn = ref<HTMLButtonElement | null>(null)

// Whatever had focus when the dialog opened (the button that was pressed) gets it back on close.
const opener = document.activeElement as HTMLElement | null

// Safe default focus: the harmless choice.
onMounted(() => cancelBtn.value?.focus())
onBeforeUnmount(() => { if (opener && document.contains(opener)) opener.focus() })

function trapFocus(e: KeyboardEvent) {
  const els = dialog.value?.querySelectorAll<HTMLElement>('button:not(:disabled)')
  if (!els || !els.length) return
  const first = els[0]
  const last = els[els.length - 1]
  if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus() }
  else if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus() }
}
</script>

<style scoped>
.cd-overlay {
  position: fixed; inset: 0; background: rgba(10,26,16,0.6); backdrop-filter: blur(4px);
  z-index: 300; display: flex; align-items: center; justify-content: center; padding: 16px;
}
.cd-box { background: white; border-radius: 14px; width: min(420px, 100%); padding: 22px 24px; box-shadow: var(--shadow-xl); }
.cd-title { font-family: var(--font-sans); font-weight: 700; font-size: 15px; color: var(--forest); margin: 0 0 8px; }
.cd-message { font-size: 13px; color: var(--stone); line-height: 1.55; margin: 0 0 14px; }
.cd-error { font-size: 12px; color: #B42323; margin-bottom: 12px; }
.cd-actions { display: flex; justify-content: flex-end; gap: 8px; flex-wrap: wrap; }
.cd-confirm { background: #B42323; color: white; }
.cd-confirm:hover:not(:disabled) { background: #8F1B1B; }
</style>
