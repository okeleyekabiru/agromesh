<template>
  <div class="cf">
    <label class="form-label" :for="id">{{ label }}</label>
    <div class="cf-row">
      <input
        class="cf-picker" type="color" :value="picker" :disabled="disabled"
        :aria-label="`${label}, colour picker`" @input="onPick"
      />
      <input
        :id="id" class="form-input cf-text" type="text" inputmode="text" maxlength="7" spellcheck="false" autocomplete="off"
        :value="modelValue" :placeholder="fallback" :disabled="disabled"
        :aria-invalid="!!error" :aria-describedby="describedBy"
        @input="onType" @blur="onBlur"
      />
    </div>
    <div v-if="error" :id="`${id}-err`" class="cf-error" role="alert">{{ error }}</div>
    <div v-else-if="hint" :id="`${id}-hint`" class="cf-hint">{{ hint }}</div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { colorError, isHexColor, pickerValue } from '@/utils/brandColors'

const props = defineProps<{
  id: string
  label: string
  modelValue: string
  /** The colour used when this field is blank: shown in the picker and as the placeholder. */
  fallback: string
  disabled?: boolean
  hint?: string
}>()
const emit = defineEmits<{ 'update:modelValue': [value: string] }>()

const picker = computed(() => pickerValue(props.modelValue, props.fallback))
const error = computed(() => colorError(props.label, props.modelValue))
const describedBy = computed(() => (error.value ? `${props.id}-err` : props.hint ? `${props.id}-hint` : undefined))

function onPick(e: Event) { emit('update:modelValue', (e.target as HTMLInputElement).value.toUpperCase()) }
function onType(e: Event) { emit('update:modelValue', (e.target as HTMLInputElement).value) }
// Tidy a valid colour into the form the API returns (upper case); leave anything else for the message.
function onBlur() {
  const v = props.modelValue.trim()
  if (isHexColor(v) && v !== v.toUpperCase()) emit('update:modelValue', v.toUpperCase())
  else if (v !== props.modelValue) emit('update:modelValue', v)
}
</script>

<style scoped>
.cf-row { display: flex; align-items: center; gap: 8px; }
.cf-picker {
  width: 44px; height: 38px; padding: 2px; flex: none; cursor: pointer;
  border: 1px solid var(--fog); border-radius: 8px; background: #fff;
}
.cf-picker:disabled { cursor: not-allowed; opacity: 0.6; }
.cf-text { font-family: var(--font-sans); letter-spacing: 0.04em; text-transform: uppercase; max-width: 140px; }
.cf-text::placeholder { text-transform: uppercase; color: #7B8A81; }
.cf-error { margin-top: 4px; font-size: 12px; color: #B42323; line-height: 1.4; }
.cf-hint { margin-top: 4px; font-size: 11px; color: var(--stone); line-height: 1.4; }
</style>
