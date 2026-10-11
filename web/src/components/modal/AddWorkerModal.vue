<template>
  <div class="modal-overlay" @click.self="$emit('close')">
    <div class="modal">
      <div class="modal-header">
        <div class="modal-title">Add a worker</div>
        <button class="modal-close" @click="$emit('close')">×</button>
      </div>
      <div class="modal-body">
        <div class="form-group">
          <label class="form-label">Full name</label>
          <input class="form-input" v-model="name" placeholder="e.g. Ade Bello" />
        </div>
        <div class="form-group">
          <label class="form-label">Wage band</label>
          <select class="form-select" v-model="roleId" @change="onRoleChange">
            <option value="" disabled>— Select role —</option>
            <option v-for="r in store.workerRoles" :key="r.id" :value="r.id">
              {{ r.title }} — ₦{{ r.payRate.toLocaleString() }} / {{ PAY_FREQUENCY_LABELS[r.payFrequency].toLowerCase() }}
            </option>
          </select>
          <div v-if="!store.workerRoles.length" class="hint">No wage bands yet — create one during farm setup first.</div>
        </div>
        <div class="form-group">
          <label class="form-label">Type</label>
          <select class="form-select" v-model="type">
            <option value="Employee">Employee</option>
            <option value="Contractor">Contractor</option>
          </select>
        </div>
        <div v-if="error" class="error-msg">{{ error }}</div>
      </div>
      <div class="modal-footer">
        <button class="btn btn-ghost" @click="$emit('close')" :disabled="saving">Cancel</button>
        <button class="btn btn-primary" @click="submit" :disabled="!canSubmit || saving">
          {{ saving ? 'Adding…' : 'Add worker' }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useAgroStore } from '@/stores/agro'
import { PAY_FREQUENCY_LABELS, type WorkerType } from '@/types'
import { extractErrorMessage } from '@/utils/http'

// The API reads enums as ordinals (no string-enum converter is configured).
const WORKER_TYPE_ORDINAL: Record<WorkerType, number> = { Employee: 0, Contractor: 1 }

const emit = defineEmits(['close', 'saved'])
const store = useAgroStore()

const name = ref('')
const roleId = ref('')
const type = ref<WorkerType>('Employee')
const saving = ref(false)
const error = ref('')

const canSubmit = computed(() => name.value.trim().length > 0 && !!roleId.value)

// Picking a wage band pre-selects its default type; the user can still override it.
function onRoleChange() {
  const role = store.workerRoles.find(r => r.id === roleId.value)
  if (role) type.value = role.defaultType
}

function initialsOf(full: string) {
  const parts = full.trim().split(/\s+/).filter(Boolean)
  return ((parts[0]?.[0] ?? '') + (parts.length > 1 ? parts[parts.length - 1][0] : '')).toUpperCase()
}

async function submit() {
  if (!canSubmit.value) return
  saving.value = true
  error.value = ''
  try {
    await store.addWorker({
      name: name.value.trim(),
      initials: initialsOf(name.value),
      workerRoleId: roleId.value,
      type: WORKER_TYPE_ORDINAL[type.value],
    })
    await store.fetchWorkerRoles() // refresh the per-band worker counts
    emit('saved')
    emit('close')
  } catch (e) {
    error.value = extractErrorMessage(e, 'Could not add this worker. Please try again.')
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
.hint { font-size: 11px; color: var(--stone); margin-top: 6px; }
.error-msg { font-size: 11.5px; color: var(--alert); line-height: 1.5; }
</style>
