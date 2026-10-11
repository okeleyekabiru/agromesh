<template>
  <div class="modal-overlay" @click.self="$emit('close')">
    <div class="modal">
      <div class="modal-header">
        <div class="modal-title">{{ role ? `Edit rate — ${role.title}` : 'Add a wage band' }}</div>
        <button class="modal-close" @click="$emit('close')">×</button>
      </div>
      <div class="modal-body">
        <template v-if="!role">
          <div class="form-group">
            <label class="form-label">Role title</label>
            <input class="form-input" v-model="title" placeholder="e.g. Harvester" />
          </div>
          <div class="form-group">
            <label class="form-label">Default type</label>
            <select class="form-select" v-model="type">
              <option value="Employee">Employee</option>
              <option value="Contractor">Contractor</option>
            </select>
          </div>
        </template>
        <div class="form-row">
          <div class="form-group" style="flex:1">
            <label class="form-label">Pay rate (₦)</label>
            <input class="form-input" type="number" min="0" step="100" v-model.number="payRate" @wheel.prevent />
          </div>
          <div class="form-group" style="flex:1">
            <label class="form-label">Per</label>
            <select class="form-select" v-model.number="payFrequency">
              <option v-for="(label, i) in PAY_FREQUENCY_LABELS" :key="i" :value="i">{{ label }}</option>
            </select>
          </div>
        </div>
        <div v-if="role && role.workerCount" class="hint">
          This changes the rate for all {{ role.workerCount }} worker{{ role.workerCount === 1 ? '' : 's' }} in this band.
        </div>
        <div v-if="error" class="error-msg">{{ error }}</div>
      </div>
      <div class="modal-footer">
        <button class="btn btn-ghost" @click="$emit('close')" :disabled="saving">Cancel</button>
        <button class="btn btn-primary" @click="submit" :disabled="!canSubmit || saving">
          {{ saving ? 'Saving…' : role ? 'Save rate' : 'Add band' }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useAgroStore } from '@/stores/agro'
import { PAY_FREQUENCY_LABELS, type WorkerRole, type WorkerType } from '@/types'
import { extractErrorMessage } from '@/utils/http'

// Pass `role` to edit its rate; omit it to create a new band.
const props = defineProps<{ role?: WorkerRole | null }>()
const emit = defineEmits(['close'])
const store = useAgroStore()

// The API reads enums as ordinals (no string-enum converter is configured).
const WORKER_TYPE_ORDINAL: Record<WorkerType, number> = { Employee: 0, Contractor: 1 }

const title = ref('')
const type = ref<WorkerType>('Employee')
const payRate = ref<number | ''>(props.role?.payRate ?? '')
const payFrequency = ref<number>(props.role?.payFrequency ?? 0)
const saving = ref(false)
const error = ref('')

const canSubmit = computed(() =>
  Number(payRate.value) > 0 && (props.role ? true : title.value.trim().length > 0)
)

async function submit() {
  if (!canSubmit.value) return
  saving.value = true
  error.value = ''
  try {
    if (props.role) {
      await store.updateWorkerRoleRate(props.role.id, Number(payRate.value), payFrequency.value)
    } else {
      await store.createWorkerRole({
        title: title.value.trim(),
        defaultType: WORKER_TYPE_ORDINAL[type.value],
        payRate: Number(payRate.value),
        payFrequency: payFrequency.value,
      })
    }
    emit('close')
  } catch (e) {
    error.value = extractErrorMessage(e, 'Could not save this wage band. Please try again.')
  } finally {
    saving.value = false
  }
}
</script>

<style scoped>
.modal-overlay { position: fixed; inset: 0; background: rgba(10,26,16,0.6); backdrop-filter: blur(4px); z-index: 200; display: flex; align-items: center; justify-content: center; }
.modal { background: white; border-radius: 16px; width: 400px; max-width: calc(100vw - 32px); box-shadow: 0 24px 64px rgba(10,26,16,0.2); }
.modal-header { padding: 20px 24px 16px; border-bottom: 1px solid var(--fog); display: flex; align-items: center; justify-content: space-between; }
.modal-title { font-family: 'Syne', sans-serif; font-weight: 700; font-size: 15px; color: var(--forest); }
.modal-close { width: 28px; height: 28px; border-radius: 6px; border: 1px solid var(--fog); background: transparent; cursor: pointer; font-size: 18px; color: var(--stone); }
.modal-body { padding: 20px 24px; }
.modal-footer { padding: 16px 24px; border-top: 1px solid var(--fog); display: flex; justify-content: space-between; }
.form-group { margin-bottom: 16px; }
.form-row { display: flex; gap: 12px; }
.hint { font-size: 11px; color: var(--stone); margin-bottom: 12px; }
.error-msg { font-size: 11.5px; color: var(--alert); line-height: 1.5; }
</style>
