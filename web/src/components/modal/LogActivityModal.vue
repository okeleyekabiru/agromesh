<template>
  <div class="modal-overlay" @click.self="$emit('close')">
    <div class="modal">
      <div class="modal-header">
        <div class="modal-title">Log Activity</div>
        <button class="modal-close" @click="$emit('close')">×</button>
      </div>

      <!-- Stepper -->
      <div class="modal-steps">
        <div v-for="(s, i) in steps" :key="s" class="step" :class="{ active: step === i+1, done: step > i+1 }">
          <div class="step-num">{{ step > i+1 ? '✓' : i+1 }}</div>
          <div class="step-label">{{ s }}</div>
        </div>
      </div>

      <div class="modal-body">
        <!-- STEP 1: Location -->
        <template v-if="step === 1">
          <div class="form-group">
            <label class="form-label">Date</label>
            <input type="date" class="form-input" v-model="form.date">
          </div>
          <div class="form-group">
            <label class="form-label">Section</label>
            <select class="form-select" v-model="form.sectionId">
              <option value="">Choose section...</option>
              <option v-for="s in store.sections" :key="s.id" :value="s.id">
                {{ s.name }} — {{ s.crop }}
              </option>
            </select>
            <div v-if="selectedSection" class="form-hint">
              {{ selectedSection.hectares }} ha · Last activity: {{ selectedSection.lastActivityDate }}
            </div>
          </div>
          <div class="form-group">
            <label class="form-label">Block</label>
            <select class="form-select" v-model="form.blockId" :disabled="!form.sectionId">
              <option value="">Choose block...</option>
              <option value="Block A">Block A</option>
              <option value="Block B">Block B</option>
              <option value="Block C">Block C</option>
              <option value="Block D">Block D</option>
            </select>
          </div>
        </template>

        <!-- STEP 2: Activity -->
        <template v-if="step === 2">
          <div class="form-group">
            <label class="form-label">Activity Type</label>
            <div class="activity-grid">
              <button
                v-for="a in activityTypes" :key="a.name"
                class="activity-btn"
                :class="{ selected: form.activityType === a.name }"
                @click="form.activityType = a.name as any"
              >
                <div class="activity-btn-icon">{{ a.icon }}</div>
                <span class="activity-btn-name">{{ a.name }}</span>
              </button>
            </div>
          </div>
          <div v-if="form.activityType" class="form-group">
            <label class="form-label">Notes (optional)</label>
            <input class="form-input" placeholder="Any details about this activity..." v-model="form.activityNote">
          </div>
        </template>

        <!-- STEP 3: Workers & Inputs -->
        <template v-if="step === 3">
          <div class="form-group">
            <label class="form-label">Workers ({{ form.workers.length }} added)</label>
            <div class="chip-row">
              <span v-for="w in form.workers" :key="w" class="chip">
                👤 {{ w }}
                <button class="chip-remove" @click="removeWorker(w)">×</button>
              </span>
              <span v-if="!form.workers.length" class="empty-chips">No workers added yet</span>
            </div>
            <div class="add-row">
              <input class="form-input" placeholder="Worker name..." v-model="workerInput" @keydown.enter="addWorker" style="flex:1">
              <select class="form-select" v-model="workerType" style="width:130px">
                <option>Employee</option>
                <option>Contractor</option>
              </select>
              <button class="btn btn-primary" @click="addWorker">+ Add</button>
            </div>
          </div>

          <div class="form-group">
            <label class="form-label">Inputs Used ({{ form.inputs.length }} added)</label>
            <div class="chip-row">
              <span v-for="inp in form.inputs" :key="inp" class="chip">
                📦 {{ inp }}
                <button class="chip-remove" @click="removeInput(inp)">×</button>
              </span>
              <span v-if="!form.inputs.length" class="empty-chips">No inputs added yet</span>
            </div>
            <div class="add-row">
              <input class="form-input" placeholder="e.g. NPK Fertilizer" v-model="inputItem" style="flex:1">
              <input class="form-input" placeholder="Qty" v-model="inputQty" style="width:64px">
              <select class="form-select" v-model="inputUnit" style="width:72px">
                <option>kg</option>
                <option>L</option>
                <option>bags</option>
                <option>units</option>
                <option>ha</option>
              </select>
              <button class="btn btn-primary" @click="addInput">+ Add</button>
            </div>
          </div>

          <div class="form-group">
            <label class="form-label">Supervisor</label>
            <select class="form-select" v-model="form.supervisorId">
              <option value="">Select supervisor...</option>
              <option v-for="w in store.supervisors" :key="w.id" :value="w.id">
                {{ w.name }}
              </option>
            </select>
          </div>
        </template>
      </div>

      <div class="modal-footer">
        <button class="btn btn-ghost" @click="step > 1 ? step-- : $emit('close')">
          {{ step === 1 ? 'Cancel' : '← Back' }}
        </button>
        <div style="display:flex;align-items:center;gap:10px;">
          <span style="font-family:'JetBrains Mono',monospace;font-size:9px;color:var(--stone);">{{ step }} / {{ steps.length }}</span>
          <button class="btn btn-primary" @click="next" :disabled="!canProceed">
            {{ step === steps.length ? '✓ Submit Activity' : 'Continue →' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useAgroStore } from '@/stores/agro'
import type { ActivityType } from '@/types'

const emit = defineEmits(['close'])
const store = useAgroStore()

const step = ref(1)
const steps = ['Location', 'Activity', 'Workers & Inputs']

const workerInput = ref('')
const workerType = ref('Employee')
const inputItem = ref('')
const inputQty = ref('')
const inputUnit = ref('kg')

const form = ref({
  date: new Date().toISOString().split('T')[0],
  sectionId: '',
  blockId: '',
  activityType: '' as ActivityType | '',
  activityNote: '',
  workers: [] as string[],
  inputs: [] as string[],
  supervisorId: ''
})

const activityTypes = [
  { name: 'Weeding',               icon: '🌿' },
  { name: 'Spraying',              icon: '💧' },
  { name: 'Fertilizer Application',icon: '🌱' },
  { name: 'Maintenance',           icon: '🔧' },
  { name: 'Harvesting',            icon: '🌾' },
  { name: 'Clearing',              icon: '🪚' },
  { name: 'Pruning',               icon: '✂️' },
  { name: 'Pest Control',          icon: '🛡️' },
]

const selectedSection = computed(() =>
  store.sections.find(s => s.id === form.value.sectionId)
)

const canProceed = computed(() => {
  if (step.value === 1) return !!form.value.sectionId && !!form.value.blockId
  if (step.value === 2) return !!form.value.activityType
  return true
})

function next() {
  if (step.value < steps.length) {
    step.value++
  } else {
    store.logActivity({
      date: form.value.date,
      sectionId: form.value.sectionId,
      blockId: form.value.blockId,
      activityType: form.value.activityType as ActivityType,
      activityNote: form.value.activityNote,
      workers: form.value.workers,
      inputs: form.value.inputs,
      supervisorId: form.value.supervisorId
    })
    emit('close')
  }
}

function addWorker() {
  if (workerInput.value.trim()) {
    form.value.workers.push(`${workerInput.value.trim()} (${workerType.value})`)
    workerInput.value = ''
  }
}

function removeWorker(w: string) {
  form.value.workers = form.value.workers.filter(x => x !== w)
}

function addInput() {
  if (inputItem.value.trim() && inputQty.value) {
    form.value.inputs.push(`${inputQty.value}${inputUnit.value} ${inputItem.value.trim()}`)
    inputItem.value = ''
    inputQty.value = ''
  }
}

function removeInput(inp: string) {
  form.value.inputs = form.value.inputs.filter(x => x !== inp)
}
</script>

<style scoped>
.modal-overlay { position: fixed; inset: 0; background: rgba(10,26,16,0.6); backdrop-filter: blur(4px); z-index: 100; display: flex; align-items: center; justify-content: center; }
.modal { background: white; border-radius: 16px; width: 480px; max-height: 85vh; overflow-y: auto; box-shadow: 0 24px 64px rgba(10,26,16,0.2); }
.modal-header { padding: 20px 24px 16px; border-bottom: 1px solid var(--fog); display: flex; align-items: center; justify-content: space-between; }
.modal-title { font-family: 'Syne', sans-serif; font-weight: 700; font-size: 16px; color: var(--forest); letter-spacing: -0.01em; }
.modal-close { width: 28px; height: 28px; border-radius: 6px; border: 1px solid var(--fog); background: transparent; cursor: pointer; font-size: 18px; color: var(--stone); display: flex; align-items: center; justify-content: center; }
.modal-close:hover { background: var(--mist); }
.modal-steps { display: flex; padding: 16px 24px; border-bottom: 1px solid var(--fog); }
.step { flex: 1; display: flex; flex-direction: column; align-items: center; position: relative; }
.step::after { content: ''; position: absolute; top: 12px; left: 50%; width: 100%; height: 1px; background: var(--fog); }
.step:last-child::after { display: none; }
.step-num { width: 24px; height: 24px; border-radius: 50%; border: 1.5px solid var(--fog); display: flex; align-items: center; justify-content: center; font-family: 'JetBrains Mono', monospace; font-size: 10px; color: var(--stone); background: white; position: relative; z-index: 1; }
.step.active .step-num { background: var(--leaf); border-color: var(--leaf); color: white; }
.step.done .step-num { background: var(--palm); border-color: var(--palm); color: white; }
.step-label { font-family: 'JetBrains Mono', monospace; font-size: 8px; letter-spacing: 0.1em; text-transform: uppercase; color: var(--stone); margin-top: 4px; text-align: center; }
.step.active .step-label { color: var(--leaf); }
.modal-body { padding: 20px 24px; }
.modal-footer { padding: 16px 24px; border-top: 1px solid var(--fog); display: flex; justify-content: space-between; align-items: center; }
.form-group { margin-bottom: 16px; }
.activity-grid { display: grid; grid-template-columns: repeat(2,1fr); gap: 8px; }
.activity-btn { padding: 10px 12px; border: 1.5px solid var(--fog); border-radius: 8px; background: transparent; cursor: pointer; text-align: left; transition: all 0.15s; }
.activity-btn:hover { border-color: var(--palm); background: rgba(61,170,106,0.04); }
.activity-btn.selected { border-color: var(--leaf); background: rgba(45,122,79,0.06); }
.activity-btn-icon { font-size: 20px; margin-bottom: 4px; line-height: 1; }
.activity-btn-name { font-size: 11px; font-weight: 600; color: var(--forest); display: block; }
.chip-row { display: flex; flex-wrap: wrap; gap: 6px; margin-bottom: 8px; min-height: 28px; align-items: center; }
.add-row { display: flex; gap: 8px; }
.empty-chips { font-family: 'JetBrains Mono', monospace; font-size: 9px; color: var(--stone); }
</style>
