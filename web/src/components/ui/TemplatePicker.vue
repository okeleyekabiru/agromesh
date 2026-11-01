<template>
  <div class="tp" role="radiogroup" :aria-labelledby="labelledby" :aria-label="labelledby ? undefined : 'Invoice template'">
    <label
      v-for="t in templates" :key="t.key" class="tp-card"
      :class="{ 'tp-card--on': t.key === modelValue }" :data-key="t.key"
    >
      <input
        class="tp-radio" type="radio" :name="name" :value="t.key" :checked="t.key === modelValue"
        :disabled="disabled" @change="emit('update:modelValue', t.key)"
      />
      <span class="tp-thumb" aria-hidden="true">
        <span class="tp-head" :style="{ background: t.defaultPrimaryColor || fallbackPrimary }" />
        <span class="tp-line tp-line--w60" />
        <span class="tp-line" />
        <span class="tp-line tp-line--w80" />
        <span class="tp-stripe" :style="{ background: t.defaultAccentColor || fallbackAccent }" />
      </span>
      <span class="tp-name">
        {{ t.name }}
        <span v-if="t.isDefault" class="tp-badge">Default</span>
      </span>
      <span v-if="t.description" class="tp-desc">{{ t.description }}</span>
      <span v-if="t.key === modelValue" class="tp-check" aria-hidden="true">✓</span>
    </label>
  </div>
</template>

<script setup lang="ts">
import type { InvoiceTemplate } from '@/types'
import { DEFAULT_ACCENT_COLOR, DEFAULT_PRIMARY_COLOR } from '@/utils/brandColors'

withDefaults(defineProps<{
  templates: Pick<InvoiceTemplate, 'key' | 'name' | 'description' | 'isDefault' | 'defaultPrimaryColor' | 'defaultAccentColor'>[]
  modelValue: string
  /** id of the element that names this group (a heading) */
  labelledby?: string
  name?: string
  disabled?: boolean
}>(), { name: 'invoice-template', labelledby: undefined })
const emit = defineEmits<{ 'update:modelValue': [key: string] }>()

const fallbackPrimary = DEFAULT_PRIMARY_COLOR
const fallbackAccent = DEFAULT_ACCENT_COLOR
</script>

<style scoped>
.tp { display: grid; grid-template-columns: repeat(auto-fill, minmax(190px, 1fr)); gap: 10px; }
.tp-card {
  position: relative; display: flex; flex-direction: column; gap: 6px; padding: 10px; cursor: pointer;
  border: 1.5px solid var(--fog); border-radius: 10px; background: #fff;
  transition: border-color var(--dur-fast) var(--ease-io), box-shadow var(--dur-fast) var(--ease-io);
}
.tp-card:hover { border-color: var(--palm); }
.tp-card--on { border-color: var(--leaf); box-shadow: 0 0 0 1px var(--leaf); background: #F7FBF8; }
/* The real radio stays in the page (keyboard, arrow keys and screen readers) but is visually hidden. */
.tp-radio { position: absolute; opacity: 0; width: 1px; height: 1px; margin: 0; pointer-events: none; }
.tp-card:has(.tp-radio:focus-visible) { outline: 2px solid var(--leaf); outline-offset: 2px; }
.tp-card:has(.tp-radio:disabled) { cursor: not-allowed; opacity: 0.6; }

.tp-thumb {
  position: relative; display: flex; flex-direction: column; gap: 4px; height: 76px; padding: 0 8px 8px;
  border: 1px solid var(--fog); border-radius: 6px; background: #fff; overflow: hidden;
}
.tp-head { display: block; height: 18px; margin: 0 -8px 2px; }
.tp-line { display: block; height: 4px; width: 100%; border-radius: 2px; background: #D9E2DC; }
.tp-line--w60 { width: 60%; }
.tp-line--w80 { width: 80%; }
.tp-stripe { position: absolute; left: 0; right: 0; bottom: 0; height: 6px; }

.tp-name { font-family: var(--font-sans); font-size: 13px; font-weight: 700; color: var(--forest); display: flex; align-items: center; gap: 6px; flex-wrap: wrap; padding-right: 20px; }
.tp-badge { font-size: 11px; font-weight: 600; color: var(--leaf); background: var(--mist); border-radius: 999px; padding: 1px 8px; }
.tp-desc { font-size: 12px; line-height: 1.45; color: var(--stone); }
.tp-check {
  position: absolute; top: 8px; right: 8px; width: 18px; height: 18px; border-radius: 50%;
  background: var(--leaf); color: #fff; font-size: 11px; line-height: 18px; text-align: center;
}
@media (max-width: 480px) {
  .tp { grid-template-columns: minmax(0, 1fr); }
}
@media (prefers-reduced-motion: reduce) {
  .tp-card { transition: none; }
}
</style>
